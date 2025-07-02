using JBDH.Models.Kanban;
using JBDH.Services.BoardDataService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace JBDH.Controllers
{
    [Authorize]
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
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized();
            var userId = int.Parse(userIdClaim.Value);

            var boards = await _boardService.GetAllAsync(); // TODO: Filter by userId in service
            return View(boards);
        }

        // GET: /Boards/Details/5
        public async Task<IActionResult> Details(int id)
        {
            Console.WriteLine("DEBUG: BoardsController.Details called with id = " + id);
            var board = await _boardService.GetAsync(id);
            if (board == null)
                return NotFound();
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
            if (ModelState.IsValid)
            {
                try
                {
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                    if (userIdClaim == null)
                        return Unauthorized();
                    var userId = int.Parse(userIdClaim.Value);

                    board.OwnerId = userId;
                    board.CreatedAt = DateTime.UtcNow;
                    board.UpdatedAt = DateTime.UtcNow;
                    await _boardService.CreateAsync(board);
                    TempData["Success"] = "Board created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error creating board: " + ex.Message;
                }
            }
            else
            {
                TempData["Error"] = "Validation failed. Please check your input.";
            }
            return View(board);
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
                return NotFound();
            if (ModelState.IsValid)
            {
                // Set OwnerId from the logged-in user
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized();
                var userId = int.Parse(userIdClaim.Value);

                board.OwnerId = userId;

                board.UpdatedAt = DateTime.UtcNow;
                await _boardService.UpdateAsync(board);
                return RedirectToAction(nameof(Index));
            }
            return View(board);
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
            return RedirectToAction(nameof(Index));
        }
    }
}