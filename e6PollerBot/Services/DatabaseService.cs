using Microsoft.EntityFrameworkCore;

namespace e6PollerBot.Services
{
    class DatabaseService
    {
        DbContext _dbContext;

        public DatabaseService(DbContext dbContext)
        {
            _dbContext = dbContext;
        }
    }
}
