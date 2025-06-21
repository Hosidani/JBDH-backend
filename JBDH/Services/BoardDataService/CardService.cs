using System.Collections.Generic;
using System.Threading.Tasks;
using JBDH.Models.Kanban;
using JBDH.Models.Core;
using Microsoft.EntityFrameworkCore;

namespace JBDH.Services.BoardDataService
{
    public class CardService : ICardService
    {
        private readonly ApplicationDbContext _context;

        public CardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Card>> GetAllAsync()
        {
            return await _context.Cards.ToListAsync();
        }

        public async Task<Card?> GetAsync(int id)
        {
            return await _context.Cards.FindAsync(id);
        }

        public async Task<Card> CreateAsync(Card card)
        {
            _context.Cards.Add(card);
            await _context.SaveChangesAsync();
            return card;
        }

        public async Task UpdateAsync(Card card)
        {
            _context.Entry(card).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var card = await _context.Cards.FindAsync(id);
            if (card != null)
            {
                _context.Cards.Remove(card);
                await _context.SaveChangesAsync();
            }
        }
    }
} 