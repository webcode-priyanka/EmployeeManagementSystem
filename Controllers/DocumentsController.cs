using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmployeeManagementSystem.Data;
using EmployeeManagementSystem.Models;

namespace EmployeeManagementSystem.Controllers
{
    [Authorize]
    public class DocumentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly UserManager<IdentityUser> _userManager;

        public DocumentsController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _environment = environment;
            _userManager = userManager;
        }


        // =====================================================
        // UPLOAD DOCUMENT
        // Employee -> Own document only
        // Admin/HR -> Any employee
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR,Employee")]
        public async Task<IActionResult> Upload(
            int employeeId,
            string documentType,
            IFormFile file,
            string? remarks)
        {
            // -------------------------------------------------
            // Find employee
            // -------------------------------------------------

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (employee == null)
            {
                return NotFound();
            }


            // -------------------------------------------------
            // Employee can upload ONLY own document
            // -------------------------------------------------

            if (User.IsInRole("Employee"))
            {
                var user = await _userManager.GetUserAsync(User);

                if (user == null || string.IsNullOrEmpty(user.Email))
                {
                    return Unauthorized();
                }

                var loggedInEmployee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Email == user.Email);

                if (loggedInEmployee == null)
                {
                    TempData["Error"] =
                        "Your employee profile could not be found.";

                    return RedirectToAction(
                        "Index",
                        "MyProfile"
                    );
                }

                // Security check
                if (loggedInEmployee.Id != employeeId)
                {
                    return Forbid();
                }
            }


            // -------------------------------------------------
            // Validate document type
            // -------------------------------------------------

            var allowedDocumentTypes = new[]
            {
                "Aadhar Card",
                "PAN Card",
                "Resume",
                "Offer Letter",
                "Joining Letter",
                "Educational Certificate",
                "Experience Certificate",
                "Other"
            };

            if (!allowedDocumentTypes.Contains(documentType))
            {
                TempData["Error"] =
                    "Invalid document type.";

                return RedirectToAction(
                    "Index",
                    "MyProfile"
                );
            }


            // -------------------------------------------------
            // Check duplicate document
            // -------------------------------------------------

            var existingDocument = await _context.EmployeeDocuments
                .FirstOrDefaultAsync(d =>
                    d.EmployeeId == employeeId &&
                    d.DocumentType == documentType);

            if (existingDocument != null)
            {
                TempData["Error"] =
                    $"{documentType} is already uploaded.";

                return RedirectToAction(
                    "Index",
                    "MyProfile"
                );
            }


            // -------------------------------------------------
            // Validate file
            // -------------------------------------------------

            if (file == null || file.Length == 0)
            {
                TempData["Error"] =
                    "Please select a document.";

                return RedirectToAction(
                    "Index",
                    "MyProfile"
                );
            }


            // -------------------------------------------------
            // Allowed extensions
            // -------------------------------------------------

            var allowedExtensions = new[]
            {
                ".pdf",
                ".jpg",
                ".jpeg",
                ".png",
                ".doc",
                ".docx"
            };

            var extension =
                Path.GetExtension(file.FileName)
                    .ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                TempData["Error"] =
                    "Only PDF, JPG, JPEG, PNG, DOC and DOCX files are allowed.";

                return RedirectToAction(
                    "Index",
                    "MyProfile"
                );
            }


            // -------------------------------------------------
            // Maximum 5 MB
            // -------------------------------------------------

            if (file.Length > 5 * 1024 * 1024)
            {
                TempData["Error"] =
                    "Maximum file size is 5 MB.";

                return RedirectToAction(
                    "Index",
                    "MyProfile"
                );
            }


            // -------------------------------------------------
            // Create employee upload folder
            // -------------------------------------------------

            var folderPath = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "employees",
                employeeId.ToString()
            );

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }


            // -------------------------------------------------
            // Generate unique file name
            // -------------------------------------------------

            var uniqueFileName =
                Guid.NewGuid().ToString() + extension;

            var filePath =
                Path.Combine(
                    folderPath,
                    uniqueFileName
                );


            // -------------------------------------------------
            // Save physical file
            // -------------------------------------------------

            using (var stream = new FileStream(
                filePath,
                FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }


            // -------------------------------------------------
            // Save database record
            // -------------------------------------------------

            var document = new EmployeeDocument
            {
                EmployeeId = employeeId,

                DocumentType = documentType,

                FileName = file.FileName,

                FilePath =
                    "/uploads/employees/"
                    + employeeId
                    + "/"
                    + uniqueFileName,

                UploadedAt = DateTime.Now,

                VerificationStatus = "Pending",

                Remarks = remarks
            };


            _context.EmployeeDocuments.Add(document);

            await _context.SaveChangesAsync();


            // -------------------------------------------------
            // Success
            // -------------------------------------------------

            TempData["Success"] =
                $"{documentType} uploaded successfully!";


            return RedirectToAction(
                "Index",
                "MyProfile"
            );
        }


        // =====================================================
        // VERIFY DOCUMENT
        // Admin / HR
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Verify(
            int id,
            int employeeId,
            string status,
            string? remarks)
        {
            var document =
                await _context.EmployeeDocuments
                    .FirstOrDefaultAsync(d =>
                        d.Id == id &&
                        d.EmployeeId == employeeId);

            if (document == null)
            {
                return NotFound();
            }


            if (status != "Verified" &&
                status != "Rejected" &&
                status != "Pending")
            {
                status = "Pending";
            }


            document.VerificationStatus = status;

            document.Remarks = remarks;


            await _context.SaveChangesAsync();


            TempData["Success"] =
                $"Document status changed to {status} successfully!";


            return RedirectToAction(
                "Details",
                "Employees",
                new { id = employeeId }
            );
        }


        // =====================================================
        // DELETE DOCUMENT
        // Admin / HR
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Delete(
            int id,
            int employeeId)
        {
            var document =
                await _context.EmployeeDocuments
                    .FirstOrDefaultAsync(d =>
                        d.Id == id &&
                        d.EmployeeId == employeeId);

            if (document == null)
            {
                return NotFound();
            }


            // Delete physical file
            if (!string.IsNullOrEmpty(document.FilePath))
            {
                var physicalPath =
                    Path.Combine(
                        _environment.WebRootPath,
                        document.FilePath
                            .TrimStart('/')
                            .Replace(
                                "/",
                                Path.DirectorySeparatorChar.ToString()
                            )
                    );


                if (System.IO.File.Exists(physicalPath))
                {
                    System.IO.File.Delete(physicalPath);
                }
            }


            // Delete database record
            _context.EmployeeDocuments.Remove(document);

            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Document deleted successfully!";


            return RedirectToAction(
                "Details",
                "Employees",
                new { id = employeeId }
            );
        }
    }
}