using App.Server.ORM;

namespace App.Server.Services
{
    public class TestService
    {
        private readonly AppDbContext _dbContext; 

        public TestService(AppDbContext dbContext)
        {
            _dbContext = dbContext; 
        }

        public void TestMethod()
        {
            var users = _dbContext.Users.ToList();
        }

    }
}
