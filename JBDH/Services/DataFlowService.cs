using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JBDH.Models;
using JBDH.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace JBDH.Services
{
    public class DataFlowService
    {
        private readonly ApplicationDbContext _context;

        public DataFlowService(ApplicationDbContext context)
        {
            _context = context;
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

        // Add a comment to a post
        public async Task AddCommentAsync(int dataFlowId, int userId, string comment)
        {
            var parent = await _context.DataFlows.FindAsync(dataFlowId);
            if (parent == null) return; // Inlägget finns inte

            var commentFlow = new DataFlow
            {
                ContentType = "Comment",
                Content = comment,
                CreatedAt = DateTime.Now,
                UserId = userId,
                ParentDataFlowId = dataFlowId
            };

            _context.DataFlows.Add(commentFlow);
            await _context.SaveChangesAsync();
        }

        public List<DataFlow> GetAllRecursive()
        {
            // 1. Load all DataFlows from the database
            var all = _context.DataFlows.ToList();

            // 2. Build a lookup so each DataFlow knows its children
            var lookup = all.ToLookup(df => df.ParentDataFlowId);

            // 3. Recursively assign replies, ordered by CreatedAt (oldest first under each parent)
            void AssignReplies(DataFlow parent)
            {
                parent.RelatedDataFlows = lookup[parent.Id]
                    .OrderBy(df => df.CreatedAt)
                    .ToList();

                foreach (var child in parent.RelatedDataFlows)
                {
                    AssignReplies(child);
                }
            }

            // 4. Get root posts (no parent), ordered newest first
            var roots = all
                .Where(df => df.ParentDataFlowId == null)
                .OrderByDescending(df => df.CreatedAt)
                .ToList();

            foreach (var root in roots)
            {
                AssignReplies(root);
            }

            return roots;
        }
    }
}