using JBDH.Models.Kanban;
using JBDH.Models.Core;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

public class BoardDataService : IBoardDataService
{
    public Board? GetBoardWithListsAndCards(int boardId)
    {
        Board? board = null;
        using (var connection = Settings.OpenConnection())
        {
            // Load the board
            var query = "SELECT * FROM Boards WHERE Id = @Id";
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Id", boardId);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        board = new Board
                        {
                            Id = reader.GetInt32("Id"),
                            Name = reader.GetString("Name"),
                            OwnerId = reader.GetInt32("OwnerId"),
                            IsOrganizationBoard = reader.GetBoolean("IsOrganizationBoard"),
                            OrganizationId = reader.IsDBNull(reader.GetOrdinal("OrganizationId")) ? (int?)null : reader.GetInt32("OrganizationId"),
                            CreatedAt = reader.GetDateTime("CreatedAt"),
                            UpdatedAt = reader.GetDateTime("UpdatedAt"),
                            Lists = new List<KanbanList>()
                        };
                    }
                }
            }
            if (board != null)
            {
                // Load lists
                var listQuery = "SELECT * FROM Lists WHERE BoardId = @BoardId";
                using (var listCommand = new MySqlCommand(listQuery, connection))
                {
                    listCommand.Parameters.AddWithValue("@BoardId", board.Id);
                    using (var listReader = listCommand.ExecuteReader())
                    {
                        while (listReader.Read())
                        {
                            board.Lists.Add(new KanbanList
                            {
                                Id = listReader.GetInt32("Id"),
                                BoardId = listReader.GetInt32("BoardId"),
                                Name = listReader.GetString("Name"),
                                Order = listReader.GetInt32("Order"),
                                CreatedAt = listReader.GetDateTime("CreatedAt"),
                                Cards = new List<Card>()
                            });
                        }
                    }
                }
                // Load cards for each list
                foreach (var list in board.Lists)
                {
                    var cardQuery = "SELECT * FROM Cards WHERE ListId = @ListId";
                    using (var cardCommand = new MySqlCommand(cardQuery, connection))
                    {
                        cardCommand.Parameters.AddWithValue("@ListId", list.Id);
                        using (var cardReader = cardCommand.ExecuteReader())
                        {
                            while (cardReader.Read())
                            {
                                list.Cards.Add(new Card
                                {
                                    Id = cardReader.GetInt32("Id"),
                                    ListId = cardReader.GetInt32("ListId"),
                                    Title = cardReader.GetString("Title"),
                                    Description = cardReader.IsDBNull(cardReader.GetOrdinal("Description")) ? null : cardReader.GetString("Description"),
                                    Order = cardReader.IsDBNull(cardReader.GetOrdinal("Order")) ? (int?)null : cardReader.GetInt32("Order"),
                                    CreatedAt = cardReader.GetDateTime("CreatedAt"),
                                    DueDate = cardReader.IsDBNull(cardReader.GetOrdinal("DueDate")) ? (System.DateTime?)null : cardReader.GetDateTime("DueDate"),
                                    AssignedUserId = cardReader.IsDBNull(cardReader.GetOrdinal("AssignedUserId")) ? (int?)null : cardReader.GetInt32("AssignedUserId")
                                });
                            }
                        }
                    }
                }
            }
        }
        return board;
    }
}