using System;
using System.Collections.Generic;
using System.Linq;
using Substrate;
using Substrate.Nbt;
using Substrate.Core;
using System.IO;

namespace minecraft_maze
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Maze maze = new Maze(35, 35);
            maze.PrintToConsole();

            NbtWorld world = AnvilWorld.Create(@"C:\Users\Home\Documents\minecraft maze\output");

            world.Level.LevelName = "MazeLand";
            world.Level.GameType = GameType.CREATIVE;
            world.Level.Player = new Player();
            world.Level.Player.Position = new Vector3
            {
                X = 20,
                Y = 70,
                Z = 20
            };

            IChunkManager cm = world.GetChunkManager();
            for (int xi = -5; xi < 5; xi++)
            {
                for (int zi = -5; zi < 5; zi++)
                {
                    ChunkRef chunk = cm.CreateChunk(xi, zi);
                    chunk.IsTerrainPopulated = true;
                    chunk.Blocks.AutoLight = false;

                    FlattenChunk(chunk, 64);

                    chunk.Blocks.RebuildHeightMap();
                    chunk.Blocks.RebuildBlockLight();
                    chunk.Blocks.RebuildSkyLight();

                    cm.Save();
                }
            }

            var blockManager = world.GetBlockManager();
            
            maze.Print((x, z) =>
            {
                blockManager.SetBlock(x, 64, z, new AlphaBlock(BlockType.COBBLESTONE));
                blockManager.SetBlock(x, 65, z, new AlphaBlock(BlockType.COBBLESTONE));
            });
            
            world.Save();
        }

        static void FlattenChunk(ChunkRef chunk, int height)
        {
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    // Create bedrock
                    for (int y = 0; y < 2; y++)
                    {
                        chunk.Blocks.SetID(x, y, z, (int)BlockType.BEDROCK);
                    }

                    // Create stone
                    for (int y = 2; y < height - 5; y++)
                    {
                        chunk.Blocks.SetID(x, y, z, (int)BlockType.STONE);
                    }

                    // Create dirt
                    for (int y = height - 5; y < height - 1; y++)
                    {
                        chunk.Blocks.SetID(x, y, z, (int)BlockType.DIRT);
                    }

                    // Create grass
                    for (int y = height - 1; y < height; y++)
                    {
                        chunk.Blocks.SetID(x, y, z, (int)BlockType.GRASS);
                    }
                }
            }
        }
    }

    class Maze
    {
        public int Width;
        public int Height;
        public bool[,,] Cells;

        public Maze(int width, int height)
        {
            this.Width = width;
            this.Height = height;
            Cells = new bool[width, height, 2];

            Random random = new Random();

            Cell currentCell = new Cell
            {
                X = random.Next(0, width - 1),
                Y = random.Next(0, height - 1)
            };

            List<Cell> path = new List<Cell>
            {
                currentCell
            };

            bool[,] visited = new bool[width, height];
            int unvisitedCount = width * height;

            visited[currentCell.X, currentCell.Y] = true;
            unvisitedCount -= 1;

            while (unvisitedCount > 0)
            {
                List<Cell> moves = PossibleMoves(visited, currentCell);
                Cell newCell;
                if (moves.Count > 0)
                {
                    newCell = moves[random.Next(moves.Count)];
                }
                else
                {
                    currentCell = Backtrack(visited, path);
                    continue;
                }

                path.Add(newCell);

                LinkCells(currentCell, newCell);

                visited[newCell.X, newCell.Y] = true;
                unvisitedCount -= 1;

                currentCell = newCell;
            }
        }

        public void PrintToConsole()
        {
            Console.BackgroundColor = ConsoleColor.White;

            Print((int i, int j) =>
            {
                Console.SetCursorPosition(i, j);
                Console.Write(" ");
            });

            Console.SetCursorPosition(0, Height * 2 + 1);
            Console.BackgroundColor = ConsoleColor.Black;
        }

        public void Print(Action<int, int> placeWall)
        {
            for (int i = 0; i < Width * 2 + 1; i++)
            {
                placeWall(i, 0);
                placeWall(i, Height * 2);
            }

            for (int j = 0; j < Height * 2 - 1; j++)
            {
                if (j != 0)
                    placeWall(0, j + 1);

                if (j != Height * 2 - 2)
                    placeWall(Width * 2, j + 1);
            }

            for (int j = 0; j < Height - 1; j++)
            {
                for (int i = 0; i < Width - 1; i++)
                {
                    placeWall(i * 2 + 2, j * 2 + 2);
                }
            }

            Cells[Width - 1, Height - 1, (int)Direction.East] = true;

            for (int j = 0; j < Height; j++)
            {
                for (int i = 0; i < Width; i++)
                {
                    if (Cells[i, j, (int)Direction.East] == false)
                    {
                        placeWall(i * 2 + 2, j * 2 + 1);
                    }

                    if (Cells[i, j, (int)Direction.North] == false)
                    {
                        placeWall(i * 2 + 1, j * 2 + 2);
                    }
                }
            }
        }

        private Cell Backtrack(bool[,] visited, List<Cell> path)
        {
            path.RemoveAt(path.Count - 1);
            while (path.Count > 0)
            {
                Cell last = path[path.Count - 1];
                List<Cell> moves = PossibleMoves(visited, last);
                if (moves.Count > 0)
                {
                    return last;
                }
                else
                {
                    path.RemoveAt(path.Count - 1);
                }
            }

            return null;
        }

        private List<Cell> PossibleMoves(bool[,] visited, Cell currentCell)
        {
            List<Cell> moves = new List<Cell>();
            var directions = new List<Func<Cell, Cell>>
            {
                North,
                South,
                East,
                West
            };

            foreach (var direction in directions)
            {
                Cell newCell = direction(currentCell);
                if (newCell.X < 0 || newCell.X >= visited.GetLength(0))
                {
                    continue;
                }

                if (newCell.Y < 0 || newCell.Y >= visited.GetLength(1))
                {
                    continue;
                }

                if (visited[newCell.X, newCell.Y])
                {
                    continue;
                }

                moves.Add(newCell);
            }

            return moves;
        }

        private Cell North(Cell cell)
        {
            return new Cell
            {
                X = cell.X,
                Y = cell.Y + 1
            };
        }

        private Cell South(Cell cell)
        {
            return new Cell
            {
                X = cell.X,
                Y = cell.Y - 1
            };
        }

        private Cell East(Cell cell)
        {
            return new Cell
            {
                X = cell.X + 1,
                Y = cell.Y
            };
        }

        private Cell West(Cell cell)
        {
            return new Cell
            {
                X = cell.X - 1,
                Y = cell.Y
            };
        }

        private void LinkCells(Cell first, Cell second)
        {
            if (first.X != second.X)
            {
                Cell minCell = first.X < second.X ? first : second;
                Cells[minCell.X, minCell.Y, (int)Direction.East] = true;
            }

            if (first.Y != second.Y)
            {
                Cell minCell = first.Y < second.Y ? first : second;
                Cells[minCell.X, minCell.Y, (int)Direction.North] = true;
            }
        }
    }

    enum Direction
    {
        North = 0,
        East = 1
    }

    class Cell
    {
        public int X;
        public int Y;
    }
}
