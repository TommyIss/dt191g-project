using dt191g_project.Data;
using dt191g_project.Models;
using Microsoft.EntityFrameworkCore;

namespace dt191g_project.Services
{
    public class UserCompanyService
    {
        private readonly ApplicationDbContext _context;

        public UserCompanyService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CompanyUser?> GetCompanyUserAsync(string userId)
        {
            return await _context.CompanyUsers
                .FirstOrDefaultAsync(x => x.UserId == userId);
        }

        public async Task<int?> GetCompanyIdForUserAsync(string userId)
        {
            var companyUser = await _context.CompanyUsers
                .FirstOrDefaultAsync(x => x.UserId == userId);

            return companyUser?.CompanyId;
        }

        public async Task<string?> GetRoleAsync(string userId)
        {
            var companyUser = await GetCompanyUserAsync(userId);

            return companyUser?.Role;
        }
    }
}
