using JBDH.Models.Kanban;
using JBDH.Models.Core;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JBDH.Services.ListService
{
    public class ListService : IListService
    {
        private readonly ApplicationDbContext _context;
        public ListService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<KanbanList> CreateAsync(KanbanList list)
        {
            _context.Lists.Add(list);
            await _context.SaveChangesAsync();
            return list;
        }

        public async Task<KanbanList> EditAsync(int id, KanbanList list)
        {
            var existing = await _context.Lists.FindAsync(id);
            if (existing == null) return null;
            existing.Name = list.Name;
            existing.Order = list.Order;
            // Update other fields as needed
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task DeleteAsync(int id)
        {
            var list = await _context.Lists.FindAsync(id);
            if (list != null)
            {
                _context.Lists.Remove(list);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<KanbanList?> GetAsync(int id)
        {
            return await _context.Lists.FindAsync(id) as KanbanList;
        }

        public async Task<IEnumerable<KanbanList>> GetListsByBoardIdAsync(int boardId)
        {
            return await _context.Lists.Where(l => l.BoardId == boardId).ToListAsync();
        }
    }
}