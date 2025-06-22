using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JBDH.Models.Kanban;
using JBDH.Services.ListService;
using System.Threading.Tasks;

namespace JBDH.Controllers
{
    [Authorize]
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
            var model = new KanbanList { BoardId = boardId };
            return View(model);
        }

        // POST: /Lists/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(KanbanList list)
        {
            Console.WriteLine("--- ListsController.Create POST ---");
            Console.WriteLine("DEBUG 1: BoardId from submitted form is: " + list.BoardId);

            if (ModelState.IsValid)
            {
                Console.WriteLine("DEBUG 2: ModelState is VALID.");
                await _listService.CreateAsync(list);

                Console.WriteLine("DEBUG 3: BoardId after saving is: " + list.BoardId);
                var redirectId = list.BoardId;

                Console.WriteLine("DEBUG 4: Redirecting to Boards/Details/" + redirectId);
                return RedirectToAction("Details", "Boards", new { id = redirectId });
            }
            else
            {
                Console.WriteLine("DEBUG 5: ModelState is INVALID.");
                foreach (var key in ModelState.Keys)
                {
                    foreach (var error in ModelState[key].Errors)
                    {
                        Console.WriteLine($"ModelState error for '{key}': {error.ErrorMessage}");
                    }
                }
                return View(list);
            }
        }

        // GET: /Lists/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var list = await _listService.GetAsync(id);
            if (list == null)
                return NotFound();
            return View(list);
        }

        // POST: /Lists/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, KanbanList list)
        {
            await _listService.EditAsync(id, list);
            return RedirectToAction("Details", "Boards", new { id = list.BoardId });
        }

        // GET: /Lists/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var list = await _listService.GetAsync(id);
            if (list == null)
                return NotFound();
            return View(list);
        }

        // POST: /Lists/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _listService.DeleteAsync(id);
            return RedirectToAction("Index", "Boards");
        }
    }
} 