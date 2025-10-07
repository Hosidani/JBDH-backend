using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JBDH.Models.Kanban;
using JBDH.Services.BoardDataService;
using JBDH.Services.ListService;
using JBDH.Helpers;

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
            return await this.GetOrError(
                () => _listService.GetAsync(listId),
                "The list you are trying to add a card to could not be found.",
                list => {
                    var card = new Card { ListId = listId, Title = string.Empty };
                    ViewBag.BoardId = list?.BoardId ?? 0;
                    return View(card);
                });
        }

        // POST: /Cards/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Card card)
        {
            return await this.ValidateModelOrReturnViewAsync(card, "Validation failed. Please check your input.", async () =>
            {
                var list = await _listService.GetAsync(card.ListId);
                if (list == null)
                    return this.ErrorPage("The list for this card could not be found.");

                card.SetTimestamps();
                await _cardService.CreateAsync(card);
                return this.SetSuccessAndRedirect("Card created successfully!", "Details", new { controller = "Boards", id = list.BoardId });
            });
        }

        // GET: /Cards/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            return await this.GetOrError(
                () => _cardService.GetAsync(id),
                "Card not found.",
                card => View(card));
        }

        // POST: /Cards/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Card card)
        {
            if (id != card.Id)
                return this.ErrorPage("Card ID mismatch.");

            return await this.ValidateModelOrReturnViewAsync(card, "Validation failed. Please check your input.", async () =>
            {
                card.SetTimestamps();
                await _cardService.UpdateAsync(card);
                return this.SetSuccessAndRedirect("Card updated successfully!", "Details", new { controller = "Boards", id = card.BoardId });
            });
        }

        // GET: /Cards/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            return await this.GetOrError(
                () => _cardService.GetAsync(id),
                "Card not found.",
                card => View(card));
        }

        // POST: /Cards/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var card = await _cardService.GetAsync(id);
            if (card == null)
                return this.ErrorPage("Card not found.");

            int boardId = card.BoardId;
            await _cardService.DeleteAsync(id);
            return this.SetSuccessAndRedirect("Card deleted successfully!", "Details", new { controller = "Boards", id = boardId });
        }
    }
}