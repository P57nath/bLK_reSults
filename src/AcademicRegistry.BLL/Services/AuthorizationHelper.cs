using System;
using System.Collections.Generic;

namespace AcademicRegistry.BLL.Services
{
    public enum Role
    {
        Admin,
        Faculty
    }

    /// <summary>
    /// MOCK AUTHORIZATION — explicitly a simplified stand-in, per Ground Rule #3 / PROMPT.md
    /// Phase 4 item 3. A real ASP.NET Identity provider (password hashing, session security,
    /// account lockout, the works) is out of scope for this learning project. This class is
    /// only enough to let Admin/Faculty-gated actions have something real to check against —
    /// it is NOT how you would authenticate users in production. External Verifiers need no
    /// entry here at all: that portal is intentionally anonymous (see PLAN.md roles table).
    /// </summary>
    public static class AuthorizationHelper
    {
        private static readonly Dictionary<string, Role> MockUsers = new Dictionary<string, Role>(StringComparer.OrdinalIgnoreCase)
        {
            { "admin", Role.Admin },
            { "faculty", Role.Faculty },
        };

        public static bool TryGetRole(string username, out Role role)
        {
            return MockUsers.TryGetValue(username ?? string.Empty, out role);
        }

        public static bool IsInRole(string username, Role requiredRole)
        {
            return TryGetRole(username, out var role) && role == requiredRole;
        }
    }
}
