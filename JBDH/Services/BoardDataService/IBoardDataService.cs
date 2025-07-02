using JBDH.Models.Kanban;

public interface IBoardDataService
{
    Board? GetBoardWithListsAndCards(int boardId);
}