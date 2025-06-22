using System.Collections.Generic;
using System.Threading.Tasks;
using JBDH.Models.Kanban;
using JBDH.Models.Core;
using Microsoft.EntityFrameworkCore;

namespace JBDH.Services.BoardDataService
{
    public class BoardService : IBoardService
    {
        private readonly ApplicationDbContext _context;
        public BoardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Board>> GetAllAsync()
        {
            return await _context.Boards.ToListAsync();
        }

        public async Task<Board?> GetAsync(int id)
        {
            return await _context.Boards
                .Include(b => b.Lists)
                .ThenInclude(l => l.Cards)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task CreateAsync(Board board)
        {
            _context.Boards.Add(board);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Board board)
        {
            _context.Boards.Update(board);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var board = await _context.Boards.FindAsync(id);
            if (board != null)
            {
                _context.Boards.Remove(board);
                await _context.SaveChangesAsync();
            }
        }
    }
} 