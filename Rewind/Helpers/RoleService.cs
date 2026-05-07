namespace Rewind.Helpers
{
    public static class RoleService
    {
        public static List<Role> GetAllRoles()
        {
            using var db = new AppDbContext();
            return db.Roles.ToList();
        }

        public static Role? GetRoleById(int id)
        {
            using var db = new AppDbContext();
            return db.Roles.FirstOrDefault(r => r.RoleId == id);
        }

        public static Role? GetRoleByName(string name)
        {
            using var db = new AppDbContext();
            return db.Roles.FirstOrDefault(r => r.RoleName == name);
        }
    }
}
