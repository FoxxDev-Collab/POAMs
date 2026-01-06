using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using POAMs.Web.Models.Domain;

namespace POAMs.Web.Data;

public static class DbInitializer
{
    /// <summary>
    /// Initialize the database with default admin user and system.
    /// NIST catalog and documentation requirements should be imported via Admin > Data Import.
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="autoSeedFromFiles">If true, attempt to auto-seed NIST data from local files (development only)</param>
    /// <param name="catalogPath">Optional path to catalog.json for auto-seeding</param>
    /// <param name="docMatrixPath">Optional path to NIST_Documentation_Matrix.csv for auto-seeding</param>
    public static async Task InitializeAsync(ApplicationDbContext context, bool autoSeedFromFiles = false, string? catalogPath = null, string? docMatrixPath = null)
    {
        // Only auto-seed from files if explicitly enabled (development scenarios)
        if (autoSeedFromFiles)
        {
            // Seed NIST catalog if not already done
            if (!await context.NISTControls.AnyAsync())
            {
                await SeedNISTCatalogAsync(context, catalogPath);
            }

            // Seed documentation requirements if not already done
            if (!await context.ControlDocumentationRequirements.AnyAsync() && await context.NISTControls.AnyAsync())
            {
                await SeedDocumentationRequirementsAsync(context, docMatrixPath);
            }
        }
        else
        {
            // Production mode: Check if data needs to be imported
            if (!await context.NISTControls.AnyAsync())
            {
                Console.WriteLine("Note: NIST controls not found. Use Admin > Data Import to import the NIST 800-53 catalog.");
            }
            if (!await context.ControlDocumentationRequirements.AnyAsync() && await context.NISTControls.AnyAsync())
            {
                Console.WriteLine("Note: Documentation requirements not found. Use Admin > Data Import to import the Documentation Matrix.");
            }
        }

        // Check if we already have users
        if (await context.Users.AnyAsync())
            return;

        // Create default admin user (local account)
        var adminUser = new User
        {
            Username = "admin",
            Email = "admin@localhost",
            DisplayName = "System Administrator",
            PasswordHash = HashPassword("ChangeMe123!@#"), // Default password - should be changed immediately
            Role = UserRole.Admin,
            IsWindowsAuth = false,
            IsActive = true
        };

        context.Users.Add(adminUser);
        await context.SaveChangesAsync();

        // Create a default system
        var defaultSystem = new SystemInfo
        {
            SystemName = "Default System",
            OrganizationName = "Your Organization",
            ISType = "Major Application",
            UID = "SYS-001"
        };

        context.Systems.Add(defaultSystem);
        await context.SaveChangesAsync();
    }

    private static async Task SeedNISTCatalogAsync(ApplicationDbContext context, string? catalogPath)
    {
        // Default to nist_catalog/catalog.json in project root
        catalogPath ??= Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "nist_catalog", "catalog.json");

        // Try alternate paths if not found
        if (!File.Exists(catalogPath))
        {
            catalogPath = Path.Combine(Directory.GetCurrentDirectory(), "nist_catalog", "catalog.json");
        }
        if (!File.Exists(catalogPath))
        {
            catalogPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "nist_catalog", "catalog.json");
        }

        if (!File.Exists(catalogPath))
        {
            Console.WriteLine($"Warning: NIST catalog not found. Tried paths ending with nist_catalog/catalog.json");
            return;
        }

        Console.WriteLine($"Loading NIST catalog from: {catalogPath}");

        var json = await File.ReadAllTextAsync(catalogPath);
        var catalog = JsonSerializer.Deserialize<Dictionary<string, CatalogEntry>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (catalog == null)
        {
            Console.WriteLine("Warning: Failed to parse NIST catalog JSON");
            return;
        }

        Console.WriteLine($"Seeding {catalog.Count} NIST controls...");

        foreach (var (controlId, entry) in catalog)
        {
            // Parse family from control ID (e.g., "AC" from "AC-2(1)")
            var family = controlId.Split('-')[0];

            // Check if withdrawn
            var isWithdrawn = entry.ControlText?.Contains("[Withdrawn:") ?? false;

            // Parse parent control ID for enhancements (e.g., "AC-2" from "AC-2(1)")
            string? parentControlId = null;
            if (controlId.Contains('('))
            {
                parentControlId = controlId.Substring(0, controlId.IndexOf('('));
            }

            var control = new NISTControl
            {
                ControlId = controlId,
                Family = family,
                Name = entry.Name ?? string.Empty,
                ControlText = entry.ControlText ?? string.Empty,
                Discussion = entry.Discussion,
                RelatedControls = entry.RelatedControls != null
                    ? JsonSerializer.Serialize(entry.RelatedControls)
                    : null,
                IsWithdrawn = isWithdrawn,
                ParentControlId = parentControlId
            };

            context.NISTControls.Add(control);
            await context.SaveChangesAsync();

            // Add CCIs for this control
            if (entry.CCIs != null)
            {
                foreach (var cciEntry in entry.CCIs)
                {
                    var cci = new CCI
                    {
                        NISTControlId = control.Id,
                        CCINumber = cciEntry.CCI ?? string.Empty,
                        Definition = cciEntry.Definition ?? string.Empty
                    };
                    context.CCIs.Add(cci);
                }
                await context.SaveChangesAsync();
            }
        }

        Console.WriteLine($"NIST catalog seeding complete. {await context.NISTControls.CountAsync()} controls, {await context.CCIs.CountAsync()} CCIs.");
    }

    private static async Task SeedDocumentationRequirementsAsync(ApplicationDbContext context, string? csvPath)
    {
        // Default to nist_catalog/NIST_Documentation_Matrix.csv in project root
        csvPath ??= Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "nist_catalog", "NIST_Documentation_Matrix.csv");

        // Try alternate paths if not found
        if (!File.Exists(csvPath))
        {
            csvPath = Path.Combine(Directory.GetCurrentDirectory(), "nist_catalog", "NIST_Documentation_Matrix.csv");
        }
        if (!File.Exists(csvPath))
        {
            csvPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "nist_catalog", "NIST_Documentation_Matrix.csv");
        }

        if (!File.Exists(csvPath))
        {
            Console.WriteLine($"Warning: Documentation matrix CSV not found. Tried paths ending with nist_catalog/NIST_Documentation_Matrix.csv");
            return;
        }

        Console.WriteLine($"Loading documentation requirements from: {csvPath}");

        // Build lookup of control IDs to database IDs
        var controlLookup = await context.NISTControls
            .ToDictionaryAsync(c => c.ControlId, c => c.Id);

        // Track what we've already added (to handle duplicates in CSV)
        var existingKeys = await context.ControlDocumentationRequirements
            .Select(d => new { d.NISTControlId, d.DocType })
            .ToListAsync();
        var processedKeys = new HashSet<(int, DocumentationType)>(
            existingKeys.Select(e => (e.NISTControlId, e.DocType)));

        var lines = await File.ReadAllLinesAsync(csvPath);
        var seededCount = 0;
        var skippedCount = 0;

        // Skip header row
        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Parse CSV line (handle quoted fields with commas)
            var fields = ParseCsvLine(line);
            if (fields.Count < 5)
                continue;

            var controlId = fields[1].Trim(); // Control ID column
            var docTypeStr = fields[3].Trim(); // Doc Type column
            var priorityStr = fields[4].Trim(); // Priority column
            var notes = fields.Count > 11 ? fields[11].Trim().Trim('"') : null; // Notes column

            // Find the control
            if (!controlLookup.TryGetValue(controlId, out var nistControlId))
            {
                skippedCount++;
                continue;
            }

            // Parse doc type
            if (!Enum.TryParse<DocumentationType>(docTypeStr, true, out var docType))
            {
                skippedCount++;
                continue;
            }

            // Check if we've already processed this control+doctype combination
            var key = (nistControlId, docType);
            if (processedKeys.Contains(key))
            {
                skippedCount++;
                continue;
            }

            // Mark as processed
            processedKeys.Add(key);

            // Parse priority
            var priority = priorityStr.ToLower() switch
            {
                "critical" => DocumentationPriority.Critical,
                "high" => DocumentationPriority.High,
                "medium" => DocumentationPriority.Medium,
                "low" => DocumentationPriority.Low,
                _ => DocumentationPriority.Medium
            };

            var requirement = new ControlDocumentationRequirement
            {
                NISTControlId = nistControlId,
                DocType = docType,
                Priority = priority,
                Description = notes
            };

            context.ControlDocumentationRequirements.Add(requirement);
            seededCount++;

            // Batch save every 100 records
            if (seededCount % 100 == 0)
            {
                await context.SaveChangesAsync();
            }
        }

        await context.SaveChangesAsync();
        Console.WriteLine($"Documentation requirements seeding complete. {seededCount} requirements added, {skippedCount} skipped.");
    }

    /// <summary>
    /// Parse a CSV line handling quoted fields with commas
    /// </summary>
    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        foreach (var c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        fields.Add(current.ToString());
        return fields;
    }

    // Helper classes for JSON deserialization
    private class CatalogEntry
    {
        public string? Name { get; set; }
        public string? ControlText { get; set; }
        public string? Discussion { get; set; }
        public List<string>? RelatedControls { get; set; }
        public List<CCIEntry>? CCIs { get; set; }
    }

    private class CCIEntry
    {
        public string? CCI { get; set; }
        public string? Definition { get; set; }
    }

    private static string HashPassword(string password)
    {
        // Simple hash for initial seed - real hashing is done in UserService
        using var deriveBytes = new System.Security.Cryptography.Rfc2898DeriveBytes(
            password,
            16,
            100000,
            System.Security.Cryptography.HashAlgorithmName.SHA256);

        byte[] salt = deriveBytes.Salt;
        byte[] hash = deriveBytes.GetBytes(32);

        byte[] result = new byte[48];
        Buffer.BlockCopy(salt, 0, result, 0, 16);
        Buffer.BlockCopy(hash, 0, result, 16, 32);
        return Convert.ToBase64String(result);
    }
}
