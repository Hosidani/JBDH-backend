using System.Collections.Generic;
using System.Threading.Tasks;
using JBDH.Models.Kanban;

namespace JBDH.Services.BoardDataService
{
    public interface ICardService
    {
        Task<IEnumerable<Card>> GetAllAsync();
        Task<Card?> GetAsync(int id);
        Task<Card> CreateAsync(Card card);
        Task UpdateAsync(Card card);
        Task DeleteAsync(int id);
    }
} 