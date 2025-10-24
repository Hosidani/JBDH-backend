using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JBDH.Swagger;
using JBDH.Models.Kanban;
using JBDH.Services.ListService;
using JBDH.Helpers;

namespace JBDH.Controllers
{
    [Authorize]
    [IncludeInApi]
    public class ListsController : Controller
    {
        private readonly IListService _listService;
        public ListsController(IListService listService)
        {
            _listService = listService;
        }

        // GET: /Lists/Create?boardId=5
        public IActionResult Create(int boardId)
        {
            var model = new KanbanList { BoardId = boardId, Name = string.Empty };
            return View(model);
        }

        // POST: /Lists/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(KanbanList list)
        {
            return await this.ValidateModelOrReturnViewAsync(list, "Validation failed. Please check your input.", async () =>
            {
                await _listService.CreateAsync(list);
                return this.SetSuccessAndRedirect("List created successfully!", "Details", new { controller = "Boards", id = list.BoardId });
            });
        }

        // GET: /Lists/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            return await this.GetOrError(
                () => _listService.GetAsync(id),
                "List not found.",
                list => View(list));
        }

        // POST: /Lists/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, KanbanList list)
        {
            var updatedList = await _listService.EditAsync(id, list);
            if (updatedList == null)
                return this.ErrorPage("List not found.");
            return this.SetSuccessAndRedirect("List updated successfully!", "Details", new { controller = "Boards", id = updatedList.BoardId });
        }

        // GET: /Lists/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            return await this.GetOrError(
                () => _listService.GetAsync(id),
                "List not found.",
                list => View(list));
        }

        // POST: /Lists/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _listService.DeleteAsync(id);
            return this.SetSuccessAndRedirect("List deleted successfully!", "Index", new { controller = "Boards" });
        }
    }
}