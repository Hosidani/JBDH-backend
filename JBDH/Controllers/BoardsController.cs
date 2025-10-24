using JBDH.Models.Kanban;
using JBDH.Services.BoardDataService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JBDH.Swagger;
using JBDH.Helpers;

namespace JBDH.Controllers
{
    [Authorize]
    [IncludeInApi]
    public class BoardsController : Controller
    {
        private readonly IBoardService _boardService;

        public BoardsController(IBoardService boardService)
        {
            _boardService = boardService;
        }

        // GET: /Boards
        public async Task<IActionResult> Index()
        {
            var authResult = this.EnsureAuthenticatedUserOrRedirect();
            if (authResult.ErrorResult != null)
                return authResult.ErrorResult;

            if (!authResult.Value.HasValue)
                return this.ErrorPage("Authenticated user ID is missing.");

            var boards = await _boardService.GetAllAsync(); // TODO: Filter by userId in service
            return View(boards);
        }

        // GET: /Boards/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var board = await _boardService.GetAsync(id);
            if (board == null)
                return this.ErrorPage("Board not found.");
            return View(board);
        }

        // GET: /Boards/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Boards/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Board board)
        {
            if (!ModelState.IsValid)
            {
                this.SetModelError(string.Empty, "Validation failed. Please check your input.");
                return View(board);
            }

            var authResult = this.EnsureAuthenticatedUserOrRedirect();
            if (authResult.ErrorResult != null)
                return authResult.ErrorResult;

            if (!authResult.Value.HasValue)
                return this.ErrorPage("Authenticated user ID is missing.");

            try
            {
                board.OwnerId = authResult.Value.Value;
                board.CreatedAt = DateTime.UtcNow;
                board.UpdatedAt = DateTime.UtcNow;
                await _boardService.CreateAsync(board);
                return this.SetSuccessAndRedirect("Board created successfully!", nameof(Index));
            }
            catch (Exception ex)
            {
                return this.ErrorPage("Error creating board: " + ex.Message);
            }
        }

        // GET: /Boards/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var board = await _boardService.GetAsync(id);
            if (board == null)
                return NotFound();
            return View(board);
        }

        // POST: /Boards/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Board board)
        {
            if (id != board.Id)
                return this.ErrorPage("Board ID mismatch.");

            if (!ModelState.IsValid)
            {
                this.SetModelError(string.Empty, "Validation failed. Please check your input.");
                return View(board);
            }

            var authResult = this.EnsureAuthenticatedUserOrRedirect();
            if (authResult.ErrorResult != null)
                return authResult.ErrorResult;

            if (!authResult.Value.HasValue)
                return this.ErrorPage("Authenticated user ID is missing.");

            board.OwnerId = authResult.Value.Value;
            board.UpdatedAt = DateTime.UtcNow;
            await _boardService.UpdateAsync(board);
            return this.SetSuccessAndRedirect("Board updated successfully!", nameof(Index));
        }

        // GET: /Boards/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var board = await _boardService.GetAsync(id);
            if (board == null)
                return NotFound();
            return View(board);
        }

        // POST: /Boards/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _boardService.DeleteAsync(id);
            return this.SetSuccessAndRedirect("Board deleted successfully!", nameof(Index));
        }
    }
}