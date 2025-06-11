using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JBDH.Models;
using JBDH.Models.ViewModels;

namespace JBDH.Services
{
    public class DataFlowService
    {
        private readonly ApplicationDbContext _context;

        public DataFlowService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Get all posts
        public List<DataFlow> GetAll()
        {
            return _context.DataFlows.OrderByDescending(df => df.CreatedAt).ToList();
        }

        // Get posts for a specific user
        public List<DataFlow> GetByUserId(int userId)
        {
            return _context.DataFlows.Where(df => df.UserId == userId).OrderByDescending(df => df.CreatedAt).ToList();
        }

        // Create a new post
        public async Task CreateAsync(DataFlowViewModel model, int userId)
        {
            // Map the view model to the DataFlow model
            var dataFlow = new DataFlow
            {
                ContentType = model.ContentType,
                Content = model.Content,
                CreatedAt = DateTime.Now,
                UserId = userId // Set the UserId from the view model
            };

            // Add to the database
            _context.DataFlows.Add(dataFlow);
            await _context.SaveChangesAsync();
        }

        // (Optional) Delete a post
        public async Task DeleteAsync(int id)
        {
            var post = _context.DataFlows.Find(id);
            if (post != null)
            {
                _context.DataFlows.Remove(post);
                await _context.SaveChangesAsync();
            }
        }
    }
}