using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JBDH.Models.Kanban;
using JBDH.Services.BoardDataService;
using JBDH.Services.ListService;
using System;
using System.Threading.Tasks;

namespace JBDH.Controllers
{
    [Authorize]
    public class CardsController : Controller
    {
        private readonly ICardService _cardService;
        private readonly IListService _listService;

        public CardsController(ICardService cardService, IListService listService)
        {
            _cardService = cardService;
            _listService = listService;
        }

        // GET: /Cards/Create?listId=5
        public async Task<IActionResult> Create(int listId)
        {
            var list = await _listService.GetAsync(listId);
            if (list == null)
            {
                return NotFound("The list you are trying to add a card to could not be found.");
            }
            
            var card = new Card { ListId = listId, Title = string.Empty };
            ViewBag.BoardId = list.BoardId; // For the cancel button
            
            return View(card);
        }

        // POST: /Cards/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Card card)
        {
            if (ModelState.IsValid)
            {
                var list = await _listService.GetAsync(card.ListId);
                if (list == null)
                {
                    return NotFound("The list for this card could not be found.");
                }
                
                card.CreatedAt = DateTime.UtcNow;
                await _cardService.CreateAsync(card);
                
                return RedirectToAction("Details", "Boards", new { id = list.BoardId });
            }
            return View(card);
        }

        // GET: /Cards/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var card = await _cardService.GetAsync(id);
            if (card == null)
                return NotFound();
            return View(card);
        }

        // POST: /Cards/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Card card)
        {
            if (id != card.Id)
                return NotFound();
            if (ModelState.IsValid)
            {
                await _cardService.UpdateAsync(card);
                // TODO: Fetch boardId from service if needed
                return RedirectToAction("Details", "Boards", new { id = card.BoardId });
            }
            return View(card);
        }

        // GET: /Cards/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var card = await _cardService.GetAsync(id);
            if (card == null)
                return NotFound();
            return View(card);
        }

        // POST: /Cards/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var card = await _cardService.GetAsync(id);
            if (card != null)
            {
                int boardId = card.BoardId; // TODO: Ensure Card has BoardId or fetch via service
                await _cardService.DeleteAsync(id);
                return RedirectToAction("Details", "Boards", new { id = boardId });
            }
            return NotFound();
        }
    }
}