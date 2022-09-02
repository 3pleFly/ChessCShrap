using System;
using System.ComponentModel;
using System.Data.Common;
using System.Net;
using System.Security.AccessControl;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Xml;

namespace ChessRewrite2
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            ChessGame.getInstance().play();
        }
    }

    class ChessGame
    {
        private static ChessGame chessGameInstance;
        private Board board;
        private Board[] boardHistory;
        private bool isWhitesTurn;

        private bool check;
        //TODO
        //3 fold, 50 moves, voluntary, dead position

        private ChessGame()
        {
            board = new Board();
            boardHistory = new Board[50];
            isWhitesTurn = true;
            check = false;
        }

        public static ChessGame getInstance()
        {
            if (chessGameInstance == null)
            {
                chessGameInstance = new ChessGame();
                return chessGameInstance;
            }

            return chessGameInstance;
        }

        public void play()
        {
            while (!IsCheckmate() && !IsStalemate())
            {
                board.PrintBoard();
                Move move = GetValidUserInput(isWhitesTurn);
                if (board.TryMove(move, isWhitesTurn))
                {
                    board.NextMove(move, isWhitesTurn);
                    check = board.IsCheck(isWhitesTurn);
                    SaveHistory();
                    isWhitesTurn = !isWhitesTurn;
                }
                else
                {
                    Console.WriteLine("Illegal Move");
                }
            }

            board.PrintBoard();
            if (check)
            {
                Console.WriteLine("Checkmate");
                Console.WriteLine(isWhitesTurn ? "Black wins!" : "White wins!");
            }
            else
            {
                Console.WriteLine("Stalemate");
            }
        }

        private void SaveHistory()
        {
            if (boardHistory[boardHistory.Length - 1] != null)
            {
                Board[] oldBoards = CloneBoardArray(boardHistory);
                boardHistory = new Board[boardHistory.Length + 50];
                for (int i = 0; i < oldBoards.Length; i++)
                {
                    boardHistory[i] = oldBoards[i].Clone();
                }
            }

            for (int i = 0; i < boardHistory.Length; i++)
            {
                if (boardHistory[i] == null)
                {
                    boardHistory[i] = board.Clone();
                }
            }
        }

        private Board[] CloneBoardArray(Board[] boards)
        {
            Board[] boardsCopy = new Board[boards.Length];
            for (int i = 0; i < boards.Length; i++)
            {
                boardsCopy[i] = boards[i].Clone();
            }

            return boardsCopy;
        }

        private bool IsCheckmate()
        {
            if (check)
            {
                return board.IsCheckmate(isWhitesTurn);
            }

            return false;
        }

        private bool IsStalemate()
        {
            if (board.IsCheckmate(isWhitesTurn) && !board.CanAnyPieceMove(isWhitesTurn))
            {
                return true;
            }

            //threefold


            //50moves

            //dead position

            return false;
        }

        private Move GetValidUserInput(bool isWhitesTurn)
        {
            Console.WriteLine(isWhitesTurn ? "White's turn." : "Black's turn.");
            string userInput = Console.ReadLine();
            while (!IsInputValid(userInput, isWhitesTurn))
            {
                Console.WriteLine("Invalid format.");
                userInput = Console.ReadLine();
            }


            userInput = ToLowerCase(userInput);
            return new Move(userInput);
        }

        private bool IsInputValid(string userInput, bool isWhitesTurns)
        {
            if (userInput.Length != 4 || !IsCharactersValid(userInput))
            {
                return false;
            }

            return true;
        }

        private bool IsCharactersValid(string userInput)
        {
            if (IsLettersValid(userInput[0]) && IsLettersValid(userInput[2]) &&
                IsNumberValid(userInput[1]) && IsNumberValid(userInput[3]))
            {
                return true;
            }

            return false;
        }

        private bool IsLettersValid(Char character)
        {
            if (character - 97 < 8 || character - 97 > -1)
                return true;
            return false;
        }

        private bool IsNumberValid(Char character)
        {
            return Char.IsNumber(character) && (int.Parse(character + "") >= 1 && int.Parse(character + "") <= 8);
        }

        private string ToLowerCase(string userInput)
        {
            char startingLower = Char.ToLower(userInput[0]);
            char endingLower = Char.ToLower(userInput[2]);
            return new string(new char[] { startingLower, userInput[1], endingLower, userInput[3] });
        }
    }

    class Log
    {
        private Log[] moveHistory;
        private int countOfMoves;
        private bool isWhitesTurn;
        private Piece piece;
        private Move move;

        private Log(bool isWhitesTurn, Piece piece, Move move, int countOfMoves)
        {
            this.countOfMoves = countOfMoves;
            this.isWhitesTurn = isWhitesTurn;
            this.piece = piece;
            this.move = move;
        }

        public Log()
        {
            this.moveHistory = new Log[50];
            this.countOfMoves = 0;
        }

        private void ExpandHistory()
        {
            Log[] newHistory = new Log[moveHistory.Length + 50];
            for (int i = 0; i < moveHistory.Length; i++)
            {
                newHistory[i] = moveHistory[i];
            }

            moveHistory = newHistory;
        }

        public void New(bool isWhitesTurns, Piece piece, Move move)
        {
            if (moveHistory[moveHistory.Length - 1] != null)
            {
                ExpandHistory();
            }

            for (int i = 0; i < moveHistory.Length; i++)
            {
                if (moveHistory[i] == null)
                {
                    moveHistory[i] = new Log(isWhitesTurns, piece.Clone(), move, ++countOfMoves);
                    break;
                }
            }
        }

        public Piece GetPiece()
        {
            return this.piece;
        }

        public Move GetMove()
        {
            return this.move;
        }

        public Log RetrieveLastLog()
        {
            for (int i = 0; i < moveHistory.Length; i++)
            {
                if (moveHistory[i] == null)
                {
                    if (i == 0)
                    {
                        return null;
                    }

                    return moveHistory[i - 1];
                }
            }

            return null;
        }
    }

    class Board
    {
        private Piece[,] pieces;
        private Log log;
        private bool enpassant;
        private bool castling;

        public Board()
        {
            log = new Log();
            castling = false;
            enpassant = false;
            this.pieces = InitializePieces();
        }

        private Board(Piece[,] pieces, Log log, bool enpassant, bool castling)
        {
            this.pieces = pieces;
            this.log = log;
            this.enpassant = enpassant;
            this.castling = castling;
        }

        public Log retrieveLastMove()
        {
            return log.RetrieveLastLog();
        }

        public Board Clone()
        {
            Piece[,] piecesCopy = Piece.ClonePieceArray(pieces);
            return new Board(piecesCopy, log, enpassant, castling);
        }

        public bool CanAnyPieceMove(bool isWhitesTurn)
        {
            for (int i = 0; i < pieces.GetLength(0); i++)
            {
                for (int j = 0; j < pieces.GetLength(1); j++)
                {
                    Piece piece = pieces[i, j];
                    if (GetPossibleMoves(new Location(i, j), isWhitesTurn).Length > 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private Move[] GetPossibleMoves(Location pieceLocation, bool isWhitesTurn)
        {
            Move[] possibilities = new Move[35];
            int possibilitiesIndex = 1;
            Piece piece = pieces[pieceLocation.GetRank(), pieceLocation.GetFile()];
            possibilities[0] = new Move(pieceLocation, pieceLocation);
            for (int k = 0; k < pieces.GetLength(0); k++)
            {
                for (int l = 0; l < pieces.GetLength(1); l++)
                {
                    Location potentialLocation = new Location(k, l);
                    Move move = new Move(pieceLocation, potentialLocation);
                    if (piece.IsLegalMove(move, pieces, isWhitesTurn))
                    {
                        possibilities[possibilitiesIndex++] = move;
                    }
                }
            }

            return possibilities;
        }

        private bool IsCastlingIllegal(Move move, bool isWhitesTurn)
        {
            Location locationDifference = move.GetLocationDifference();
            int rookIndex = locationDifference.GetFile() < 0 ? 7 : 0;
            int i = locationDifference.GetFile() < 0
                ? move.getStarting().GetFile() - 1
                : move.getStarting().GetFile() + 1;
            while (i != rookIndex)
            {
                i = locationDifference.GetFile() < 0 ? i + 1 : i - 1;
                if (i == 4 || i == 7 || i == 0 || pieces[move.getStarting().GetRank(), i] is EmptyPiece)
                {
                    if (IsLocationThreatened(new Location(move.getStarting().GetRank(), i), isWhitesTurn))
                    {
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsCheckmate(bool isWhitesTurn)
        {
            Location kingLocation = GetKingLocation(pieces, isWhitesTurn);
            if (GetPossibleMoves(kingLocation, isWhitesTurn).Length == 0)
            {
                if (!IsLocationThreatened(retrieveLastMove().GetMove().getEnding(), isWhitesTurn))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsCheck(bool isWhitesTurn)
        {
            if (IsLocationThreatened(GetKingLocation(pieces, !isWhitesTurn), !isWhitesTurn))
            {
                Console.WriteLine("CHECK");
                return true;
            }

            return false;
        }

        private bool IsLocationThreatened(Location location, bool isWhitesTurn)
        {
            for (int i = 0; i < pieces.GetLength(0); i++)
            {
                for (int j = 0; j < pieces.GetLength(1); j++)
                {
                    Move move = new Move(new Location(i, j), location);
                    Piece piece = pieces[i, j];
                    if (piece.IsLegalMove(move, pieces, !isWhitesTurn))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void Castle(Move move)
        {
            King king = (King)pieces[move.getStarting().GetRank(), move.getStarting().GetFile()];
            king.SetIsFirstMove(false);
            int rookLocationIndex = move.getEnding().GetFile() > 4 ? 7 : 0;
            int rookNewLocationIndex = rookLocationIndex > 4 ? 5 : 3;
            Rook rook = (Rook)pieces[move.getStarting().GetRank(), rookLocationIndex];
            rook.SetFirstTurn(false);
            pieces[move.getEnding().GetRank(), move.getEnding().GetFile()] = king.Clone();
            pieces[move.getStarting().GetRank(), rookNewLocationIndex] = rook.Clone();
            pieces[move.getStarting().GetRank(), rookLocationIndex] = new EmptyPiece();
            pieces[move.getStarting().GetRank(), move.getStarting().GetFile()] = new EmptyPiece();
        }

        public void NextMove(Move move, bool isWhitesTurn)
        {
            log.New(isWhitesTurn, pieces[move.getStarting().GetRank(), move.getStarting().GetFile()], move);
            if (castling)
            {
                Castle(move);
                castling = false;
                return;
            }

            if (enpassant)
            {
                Enpassant(move, isWhitesTurn);
                enpassant = false;
                return;
            }

            pieces[move.getStarting().GetRank(), move.getStarting().GetFile()].Move(move, this.pieces);
            TryPawnPromotion(move, isWhitesTurn);
        }

        private void TryPawnPromotion(Move move, bool isWhitesTurn)
        {
            Piece piece = pieces[move.getEnding().GetRank(), move.getEnding().GetFile()];
            if (piece is Pawn && IsPawnPromotion(move, isWhitesTurn))
            {
                int choice = GetUserPromotion();
                Promote(choice, move, isWhitesTurn);
            }
        }

        private void Promote(int choice, Move move, bool isWhitesTurn)
        {
            switch (choice)
            {
                case 1:
                    pieces[move.getEnding().GetRank(), move.getEnding().GetFile()] = new Queen(isWhitesTurn);
                    break;
                case 2:
                    pieces[move.getEnding().GetRank(), move.getEnding().GetFile()] = new Bishop(isWhitesTurn);
                    break;
                case 3:
                    pieces[move.getEnding().GetRank(), move.getEnding().GetFile()] = new Rook(isWhitesTurn);
                    break;
                case 4:
                    pieces[move.getEnding().GetRank(), move.getEnding().GetFile()] = new Knight(isWhitesTurn);
                    break;
            }
        }

        private int GetUserPromotion()
        {
            Console.WriteLine("Please choose. Enter a number");
            Console.WriteLine("1. Queen");
            Console.WriteLine("2. Bishop");
            Console.WriteLine("3. Rook");
            Console.WriteLine("4. Knight");
            int choice;
            do
            {
                string input = Console.ReadLine();
                int.TryParse(input, out choice);
            } while (choice == 0 || choice > 4);

            return choice;
        }

        private bool IsPawnPromotion(Move move, bool isWhitesTurn)
        {
            if ((isWhitesTurn && move.getEnding().GetRank() == 0) || (!isWhitesTurn && move.getEnding().GetRank() == 7))
            {
                return true;
            }

            return false;
        }

        public Location GetKingLocation(Piece[,] pieces, bool isWhitesTurns)
        {
            for (int i = 0; i < pieces.GetLength(0); i++)
            {
                for (int j = 0; j < pieces.GetLength(1); j++)
                {
                    if (pieces[i, j] is King && pieces[i, j].IsWhite() == isWhitesTurns)
                    {
                        return new Location(i, j);
                    }
                }
            }

            return null;
        }

        public bool TryMove(Move move, bool isWhitesTurn)
        {
            Piece piece = pieces[move.getStarting().GetRank(), move.getStarting().GetFile()];
            if (IsMoveCastling(move, pieces, isWhitesTurn) && !IsCastlingIllegal(move, isWhitesTurn))
            {
                return castling = true;
            }

            if (IsMoveEnPassant(move, isWhitesTurn) && !IsKingThreatened(move, isWhitesTurn))
            {
                return enpassant = true;
            }

            return piece.IsLegalMove(move, pieces, isWhitesTurn) && !IsKingThreatened(move, isWhitesTurn);
        }

        private bool IsKingThreatened(Move move, bool isWhitesTurn)
        {
            Piece[,] copy = Piece.ClonePieceArray(pieces);
            pieces[move.getStarting().GetRank(), move.getStarting().GetFile()].Move(move, this.pieces);
            if (IsLocationThreatened(GetKingLocation(pieces, isWhitesTurn), isWhitesTurn))
            {
                pieces = copy;
                return true;
            }

            pieces = copy;
            return false;
        }

        private void Enpassant(Move move, bool isWhitesTurn)
        {
            Pawn pawn = (Pawn)pieces[move.getStarting().GetRank(), move.getStarting().GetFile()];
            pawn.SetFirstMove(false);
            pieces[move.getStarting().GetRank(), move.getStarting().GetFile()] = new EmptyPiece();
            pieces[move.getEnding().GetRank(), move.getEnding().GetFile()] = pawn;
            pieces[isWhitesTurn ? move.getEnding().GetRank() + 1 : move.getEnding().GetRank() - 1,
                move.getEnding().GetFile()] = new EmptyPiece();
        }

        private bool IsMoveEnPassant(Move move, bool isWhitesTurn)
        {
            Piece piece = pieces[move.getStarting().GetRank(), move.getStarting().GetFile()];
            Log lastMoveLog = log.RetrieveLastLog();
            if (lastMoveLog == null || lastMoveLog.GetPiece().IsWhite() == piece.IsWhite())
            {
                return false;
            }

            Location locationDifference = move.GetLocationDifference();
            if (lastMoveLog.GetPiece() is Pawn && ((Pawn)lastMoveLog.GetPiece()).IsFirstMove())
            {
                Move lastMove = lastMoveLog.GetMove();
                if (lastMove.getEnding().GetFile() == move.getEnding().GetFile() &&
                    lastMove.getEnding().GetRank() == move.getStarting().GetRank())
                {
                    if ((isWhitesTurn && locationDifference.GetRank() > 0) ||
                        (!isWhitesTurn && locationDifference.GetRank() < 0))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void PrintBoard()
        {
            int j = 8;
            Console.WriteLine("############################");
            Console.WriteLine("   A  B  C  D  E  F  G  H ");
            for (int i = 0; i < this.pieces.GetLength(0); i++)
            {
                Console.Write(j + "  ");
                for (int k = 0; k < this.pieces.GetLength(1); k++)
                {
                    Console.Write(this.pieces[i, k] + " ");
                }

                Console.Write(" " + j--);
                Console.WriteLine();
            }

            Console.WriteLine("   A  B  C  D  E  F  G  H ");
            Console.WriteLine("############################");
        }

        private Piece[,] InitializePieces()
        {
            return new Piece[8, 8]
            {
                {
                    new Rook(false), new EmptyPiece(), new EmptyPiece(), new EmptyPiece(),
                    new King(false), new EmptyPiece(), new EmptyPiece(), new Rook(false),
                },
                {
                    new Pawn(false), new Pawn(false), new EmptyPiece(), new EmptyPiece(),
                    new Pawn(false), new Pawn(false), new Pawn(false), new Pawn(false),
                },
                {
                    new Rook(false), new EmptyPiece(), new EmptyPiece(), new EmptyPiece(),
                    new Knight(true), new EmptyPiece(), new EmptyPiece(), new EmptyPiece(),
                },
                {
                    new EmptyPiece(), new EmptyPiece(), new EmptyPiece(), new EmptyPiece(),
                    new EmptyPiece(), new EmptyPiece(), new EmptyPiece(), new EmptyPiece(),
                },
                {
                    new EmptyPiece(), new EmptyPiece(), new EmptyPiece(), new EmptyPiece(),
                    new EmptyPiece(), new EmptyPiece(), new EmptyPiece(), new EmptyPiece(),
                },
                {
                    new Queen(true), new EmptyPiece(), new EmptyPiece(), new EmptyPiece(),
                    new EmptyPiece(), new EmptyPiece(), new EmptyPiece(), new EmptyPiece(),
                },
                {
                    new Pawn(true), new Pawn(true), new Pawn(true), new Pawn(true),
                    new Pawn(true), new Pawn(true), new Pawn(true), new Pawn(true),
                },
                {
                    new Rook(true), new EmptyPiece(), new Knight(true), new EmptyPiece(),
                    new King(true), new EmptyPiece(), new EmptyPiece(), new Rook(true),
                }
            };

            // return new Piece[8, 8]
            // {
            //     {
            //         new Rook(false), new Bishop(false), new Knight(false), new Queen(false),
            //         new King(false), new Knight(false), new Bishop(false), new Rook(false),
            //     },
            //     {
            //         new Pawn(false), new Pawn(false), new Pawn(false), new Pawn(false),
            //         new Pawn(false), new Pawn(false), new Pawn(false), new Pawn(false),
            //     },
            //     {
            //         new EmptyPiece(), new EmptyPiece(), new EmptyPiece(), new EmptyPiece(),
            //         new EmptyPiece(), new EmptyPiece(), new EmptyPiece(), new EmptyPiece(),
            //     },
            //     {
            //         new EmptyPiece(), new EmptyPiece(), new EmptyPiece(), new EmptyPiece(),
            //         new EmptyPiece(), new EmptyPiece(), new EmptyPiece(), new EmptyPiece(),
            //     },
            //     {
            //         new EmptyPiece(), new EmptyPiece(), new EmptyPiece(), new EmptyPiece(),
            //         new EmptyPiece(), new EmptyPiece(), new EmptyPiece(), new EmptyPiece(),
            //     },
            //     {
            //         new EmptyPiece(), new EmptyPiece(), new EmptyPiece(), new EmptyPiece(),
            //         new EmptyPiece(), new EmptyPiece(), new EmptyPiece(), new EmptyPiece(),
            //     },
            //     {
            //         new Pawn(true), new Pawn(true), new Pawn(true), new Pawn(true),
            //         new Pawn(true), new Pawn(true), new Pawn(true), new Pawn(true),
            //     },
            //     {
            //         new Rook(true), new Bishop(true), new Knight(true), new Queen(true),
            //         new King(true), new Knight(true), new Bishop(true), new Rook(true),
            //     }
            // };
        }

        private bool IsMoveCastling(Move move, Piece[,] pieces, bool isWhitesTurn)
        {
            if ((move.getStarting().GetRank() == 0 || move.getStarting().GetRank() == 7) &&
                move.getStarting().GetFile() == 4 &&
                (move.getEnding().GetRank() == 0 || move.getEnding().GetRank() == 7) &&
                (move.getEnding().GetFile() == 6 || move.getEnding().GetFile() == 2))
            {
                Piece startingPiece = pieces[move.getStarting().GetRank(), move.getStarting().GetFile()];
                if (startingPiece is King && ((King)startingPiece).IsFirstTurn() &&
                    startingPiece.IsWhite() == isWhitesTurn)
                {
                    Location locationDifference = move.GetLocationDifference();
                    if (locationDifference.GetRank() == 0 && Math.Abs(locationDifference.GetFile()) == 2)
                    {
                        int rookIndex = locationDifference.GetFile() < 0 ? 7 : 0;
                        Piece potentialRook = pieces[move.getEnding().GetRank(), rookIndex];
                        if (potentialRook is Rook && ((Rook)potentialRook).IsFirstTurn())
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }

    public class Location
    {
        private int rank;
        private int file;

        public Location(string location)
        {
            this.file = ConvertFileToIndex(location[0]);
            this.rank = ConvertRankToIndex(location[1]);
        }

        public Location(int rank, int file)
        {
            this.rank = rank;
            this.file = file;
        }

        public Location Clone()
        {
            return new Location(this.rank, this.file);
        }

        public int GetRank()
        {
            return this.rank;
        }

        public int GetFile()
        {
            return this.file;
        }

        public void setRank(int rank)
        {
            this.rank = rank;
        }

        public void setFile(int file)
        {
            this.file = file;
        }

        public override bool Equals(object obj)
        {
            if (obj is Location)
            {
                return this.rank == ((Location)obj).rank && this.file == ((Location)obj).file;
            }

            return false;
        }

        private int ConvertFileToIndex(char file)
        {
            return file - 97;
        }

        private int ConvertRankToIndex(char rank)
        {
            return Math.Abs((int)Char.GetNumericValue(rank) - 8);
        }

        public void TraverseUp()
        {
            if (rank != 0)
            {
                rank--;
            }
        }

        public void TraverseDown()
        {
            if (rank != 7)
            {
                rank++;
            }
        }

        public void TraverseRight()
        {
            if (file != 7)
            {
                file++;
            }
        }

        public void TraverseLeft()
        {
            if (file != 0)
            {
                file--;
            }
        }
    }

    class Move
    {
        private Location starting;
        private Location ending;

        public Move(string userInput)
        {
            this.starting = new Location(new string(new char[] { userInput[0], userInput[1] }));
            this.ending = new Location(new string(new char[] { userInput[2], userInput[3] }));
        }

        public Move(Location starting, Location ending)
        {
            this.starting = starting;
            this.ending = ending;
        }

        public Move Clone()
        {
            return new Move(new Location(starting.GetRank(), starting.GetFile()),
                new Location(ending.GetRank(), ending.GetFile()));
        }

        public Location getStarting()
        {
            return this.starting;
        }

        public Location getEnding()
        {
            return this.ending;
        }

        public Location GetLocationDifference()
        {
            return new Location(this.starting.GetRank() - this.ending.GetRank(),
                this.starting.GetFile() - this.ending.GetFile());
        }
    }

    class Piece
    {
        public virtual bool IsWhite()
        {
            return false;
        }

        public virtual Piece Clone()
        {
            return new Piece();
        }

        protected virtual Location PathIterator(Move move)
        {
            return move.getStarting();
        }

        public virtual bool IsLegalMove(Move move, Piece[,] pieces, bool isWhitesTurn)
        {
            Piece startingPiece = pieces[move.getStarting().GetRank(), move.getStarting().GetFile()];
            Piece endingPiece = pieces[move.getEnding().GetRank(), move.getEnding().GetFile()];
            
            if (startingPiece is EmptyPiece || startingPiece.IsWhite() != isWhitesTurn)
            {
                return false;
            }

            if (startingPiece.IsWhite() == endingPiece.IsWhite() && !(endingPiece is EmptyPiece))
            {
                return false;
            }

            return true;
        }

        protected bool IsPathEmpty(Move move, Piece piece, Piece[,] pieces)
        {
            Move moveIterations = move.Clone();
            do
            {
                Location currentLocation = piece.PathIterator(moveIterations);
                Piece currentPiece = pieces[currentLocation.GetRank(), currentLocation.GetFile()];
                if (!(currentPiece is EmptyPiece))
                {
                    return false;
                }

                moveIterations = new Move(currentLocation, move.getEnding());
            } while (!moveIterations.getStarting().Equals(move.getEnding()));

            return true;
        }

        public virtual void Move(Move move, Piece[,] pieces)
        {
            Piece currentPiece = pieces[move.getStarting().GetRank(), move.getStarting().GetFile()];
            pieces[move.getEnding().GetRank(), move.getEnding().GetFile()] = currentPiece.Clone();
            pieces[move.getStarting().GetRank(), move.getStarting().GetFile()] = new EmptyPiece();
        }


        public static Piece[,] ClonePieceArray(Piece[,] pieces)
        {
            Piece[,] piecesCopy = new Piece[pieces.GetLength(0), pieces.GetLength(1)];
            for (int i = 0; i < pieces.GetLength(0); i++)
            {
                for (int j = 0; j < pieces.GetLength(0); j++)
                {
                    piecesCopy[i, j] = pieces[i, j].Clone();
                }
            }

            return piecesCopy;
        }
    }

    class EmptyPiece : Piece
    {
        public override string ToString()
        {
            return "  ";
        }

        public override Piece Clone()
        {
            return new EmptyPiece();
        }
    }

    class Pawn : Piece
    {
        private bool isWhite;
        private bool firstMove = true;

        public Pawn(bool isWhite)
        {
            this.isWhite = isWhite;
        }

        private Pawn(bool isWhite, bool firstMove)
        {
            this.isWhite = isWhite;
            this.firstMove = firstMove;
        }

        public override Piece Clone()
        {
            return new Pawn(isWhite, firstMove);
        }

        public bool IsFirstMove()
        {
            return this.firstMove;
        }

        public void SetFirstMove(bool value)
        {
            this.firstMove = value;
        }

        public override bool IsLegalMove(Move move, Piece[,] pieces, bool isWhitesTurn)
        {
            if (!base.IsLegalMove(move, pieces, isWhitesTurn) || !IsValidDirection(move, pieces, isWhitesTurn))
            {
                return false;
            }

            Piece endingPiece = pieces[move.getEnding().GetRank(), move.getEnding().GetFile()];
            if (move.getStarting().GetFile() == move.getEnding().GetFile() && !(endingPiece is EmptyPiece))
            {
                return false;
            }

            return true;
        }

        private bool IsValidDirection(Move move, Piece[,] pieces, bool isWhitesTurn)
        {
            Location locationDifference = move.GetLocationDifference();
            if ((locationDifference.GetRank() == 1 && isWhitesTurn ||
                 locationDifference.GetRank() == -1 && !isWhitesTurn) &&
                locationDifference.GetFile() == 0)
            {
                return true;
            }

            if ((locationDifference.GetRank() == 2 && isWhitesTurn ||
                 locationDifference.GetRank() == -2 && !isWhitesTurn) &&
                locationDifference.GetFile() == 0 && firstMove)
            {
                return true;
            }

            Piece piece = pieces[move.getEnding().GetRank(), move.getEnding().GetFile()];
            if (Math.Abs(locationDifference.GetRank()) == 1 && Math.Abs(locationDifference.GetFile()) == 1)
            {
                if (!(piece is EmptyPiece))
                {
                    return true;
                }
            }

            return false;
        }

        public override void Move(Move move, Piece[,] pieces)
        {
            this.firstMove = false;
            base.Move(move, pieces);
        }

        public override bool IsWhite()
        {
            return this.isWhite;
        }

        public override string ToString()
        {
            return isWhite ? "WP" : "BP";
        }
    }

    class Rook : Piece
    {
        private bool isWhite;
        private bool firstMove = true;

        public Rook(bool isWhite)
        {
            this.isWhite = isWhite;
        }

        private Rook(bool isWhite, bool firstMove)
        {
            this.isWhite = isWhite;
            this.firstMove = firstMove;
        }

        public override Piece Clone()
        {
            return new Rook(isWhite, firstMove);
        }

        public override bool IsLegalMove(Move move, Piece[,] pieces, bool isWhitesTurn)
        {
            return base.IsLegalMove(move, pieces, isWhitesTurn) && IsValidDirection(move) &&
                   IsPathEmpty(move, this, pieces);
        }

        protected override Location PathIterator(Move move)
        {
            Location current = move.getStarting().Clone();
            if (move.getStarting().GetRank() > move.getEnding().GetRank())
            {
                current.TraverseUp();
            }
            else if (move.getStarting().GetRank() < move.getEnding().GetRank())
            {
                current.TraverseDown();
            }
            else if (move.getStarting().GetFile() > move.getEnding().GetFile())
            {
                current.TraverseLeft();
            }
            else if (move.getStarting().GetFile() < move.getEnding().GetFile())
            {
                current.TraverseRight();
            }

            return current;
        }

        private bool IsValidDirection(Move move)
        {
            if (move.getStarting().GetFile() == move.getEnding().GetFile() ||
                move.getStarting().GetRank() == move.getEnding().GetRank())
            {
                return true;
            }

            return false;
        }

        public bool IsFirstTurn()
        {
            return this.firstMove;
        }

        public void SetFirstTurn(bool value)
        {
            this.firstMove = value;
        }

        public override void Move(Move move, Piece[,] pieces)
        {
            this.firstMove = false;
            base.Move(move, pieces);
        }

        public override bool IsWhite()
        {
            return this.isWhite;
        }

        public override string ToString()
        {
            return isWhite ? "WR" : "BR";
        }
    }

    class Bishop : Piece
    {
        private bool isWhite;

        public Bishop(bool isWhite)
        {
            this.isWhite = isWhite;
        }

        public override Piece Clone()
        {
            return new Bishop(isWhite);
        }

        public override bool IsLegalMove(Move move, Piece[,] pieces, bool isWhitesTurn)
        {
            return base.IsLegalMove(move, pieces, isWhitesTurn) && IsValidDirection(move) &&
                   IsPathEmpty(move, this, pieces);
        }

        protected override Location PathIterator(Move move)
        {
            Location changedLocation = move.getStarting().Clone();
            Location locationDifference = move.GetLocationDifference();
            if (locationDifference.GetRank() > 0 && locationDifference.GetFile() < 0)
            {
                changedLocation.TraverseUp();
                changedLocation.TraverseRight();
            }
            else if (locationDifference.GetRank() > 0 && locationDifference.GetFile() > 0)
            {
                changedLocation.TraverseUp();
                changedLocation.TraverseLeft();
            }
            else if (locationDifference.GetRank() < 0 && locationDifference.GetFile() < 0)
            {
                changedLocation.TraverseDown();
                changedLocation.TraverseRight();
            }
            else if (locationDifference.GetRank() < 0 && locationDifference.GetFile() > 0)
            {
                changedLocation.TraverseDown();
                changedLocation.TraverseLeft();
            }

            if (changedLocation.GetRank() != move.getStarting().GetRank() &&
                changedLocation.GetFile() != move.getStarting().GetFile())
                return changedLocation;
            return move.getStarting();
        }

        private bool IsValidDirection(Move move)
        {
            if (Math.Abs(move.getEnding().GetRank() - move.getStarting().GetRank()) ==
                Math.Abs(move.getStarting().GetFile() - move.getEnding().GetFile()))
            {
                return true;
            }

            return false;
        }

        public override bool IsWhite()
        {
            return this.isWhite;
        }

        public override string ToString()
        {
            return isWhite ? "WB" : "BB";
        }
    }

    class Knight : Piece
    {
        private bool isWhite;

        public Knight(bool isWhite)
        {
            this.isWhite = isWhite;
        }

        public override Piece Clone()
        {
            return new Knight(isWhite);
        }

        public override bool IsLegalMove(Move move, Piece[,] pieces, bool isWhitesTurn)
        {
            if (!base.IsLegalMove(move, pieces, isWhitesTurn))
            {
                return false;
            }

            Location locationDifference = move.GetLocationDifference();
            locationDifference.setRank(Math.Abs(locationDifference.GetRank()));
            locationDifference.setFile(Math.Abs(locationDifference.GetFile()));
            if ((locationDifference.GetRank() == 1 && locationDifference.GetFile() == 2) ||
                (locationDifference.GetRank() == 2 && locationDifference.GetFile() == 1))
            {
                return true;
            }

            return false;
        }

        public override bool IsWhite()
        {
            return this.isWhite;
        }

        public override string ToString()
        {
            return isWhite ? "WN" : "BN";
        }
    }

    class Queen : Piece
    {
        private bool isWhite;

        public Queen(bool isWhite)
        {
            this.isWhite = isWhite;
        }

        public override Piece Clone()
        {
            return new Queen(isWhite);
        }

        public override bool IsLegalMove(Move move, Piece[,] pieces, bool isWhitesTurn)
        {
            Rook rookAsQueen = new Rook(isWhite);
            Bishop bishopAsQueen = new Bishop(isWhite);
            return bishopAsQueen.IsLegalMove(move, pieces, isWhitesTurn) ||
                   rookAsQueen.IsLegalMove(move, pieces, isWhitesTurn);
        }

        public override bool IsWhite()
        {
            return this.isWhite;
        }

        public override string ToString()
        {
            return isWhite ? "WQ" : "BQ";
        }
    }

    class King : Piece
    {
        private bool isWhite;
        private bool firstMove = true;

        public King(bool isWhite)
        {
            this.isWhite = isWhite;
        }

        private King(bool isWhite, bool firstMove)
        {
            this.isWhite = isWhite;
            this.firstMove = firstMove;
        }

        public override Piece Clone()
        {
            return new King(isWhite, firstMove);
        }

        public void SetIsFirstMove(bool value)
        {
            this.firstMove = value;
        }

        public override bool IsLegalMove(Move move, Piece[,] pieces, bool isWhitesTurn)
        {
            return base.IsLegalMove(move, pieces, isWhitesTurn) && IsValidDirection(move);
        }

        public bool IsFirstTurn()
        {
            return this.firstMove;
        }

        public override void Move(Move move, Piece[,] pieces)
        {
            this.firstMove = false;
            base.Move(move, pieces);
        }

        private bool IsValidDirection(Move move)
        {
            Location locationDifference = move.GetLocationDifference();
            locationDifference.setRank(Math.Abs(locationDifference.GetRank()));
            locationDifference.setFile(Math.Abs(locationDifference.GetFile()));
            return locationDifference.GetRank() == 1 && locationDifference.GetFile() == 1 ||
                   locationDifference.GetRank() == 0 && locationDifference.GetFile() == 1 ||
                   locationDifference.GetRank() == 1 && locationDifference.GetFile() == 0;
        }

        public override bool IsWhite()
        {
            return this.isWhite;
        }

        public override string ToString()
        {
            return isWhite ? "WK" : "BK";
        }
    }
}