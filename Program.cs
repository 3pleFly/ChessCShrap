using System;
using System.ComponentModel;
using System.Data.Common;
using System.Net;
using System.Security.AccessControl;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

namespace ChessRewrite2
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            new ChessGame().play();
        }
    }

    class ChessGame
    {
        private Board board;
        private Board[] boardHistory;
        private bool isWhitesTurn;
        private bool isCheck;
        private bool checkmate;
        private bool stalemate;

        //TODO
        // castling, enpassant
        //3 fold, 50 moves, voluntary, dead position
        //can change IsAnyPieceThreateningLocation inside 'ChessGame' merge with 'King's' SelfMadeCheckmate() somehow.....

        public ChessGame()
        {
            board = new Board();
            boardHistory = new Board[50];
            isWhitesTurn = true;
            isCheck = false;
        }

        public void play()
        {
            SaveHistory();
            while (!IsCheckmate() && !IsStalemate())
            {
                board.PrintBoard();
                Move move = GetValidUserInput(isWhitesTurn);
                if (board.TryMove(move, isWhitesTurn, boardHistory))
                {
                    board.NextMove(move, isWhitesTurn, boardHistory);
                    if (board.GetIsInCheck(isWhitesTurn, boardHistory))
                    {
                        Console.WriteLine("CHECK");
                    }
                    isWhitesTurn = !isWhitesTurn;
                }
                else
                {
                    Console.WriteLine("Illegal Move");
                }
            }

            if (stalemate)
            {
                board.PrintBoard();
                Console.WriteLine("Stalemate");
            }
            else
            {
                board.PrintBoard();
                Console.WriteLine("Checkmate");
                Console.WriteLine(isWhitesTurn ? "Black wins!" : "White wins!");
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
            if (!board.KingHasLegalMoves(isWhitesTurn, boardHistory))
            {
                return true;
            }

            return false;
        }
        private bool IsStalemate()
        {
            if (!board.KingHasLegalMoves(isWhitesTurn, boardHistory) &&
                !board.CanAnyPieceMove(!isWhitesTurn, boardHistory))
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


    class Board
    {
        private Piece[,] pieces;

        public Board()
        {
            this.pieces = InitializePieces();
        }

        private Board(Piece[,] pieces)
        {
            this.pieces = pieces;
        }

        public Board Clone()
        {
            Piece[,] piecesCopy = ClonePieceArray();
            return new Board(piecesCopy);
        }

        public bool KingHasLegalMoves(bool isWhitesTurn, Board[] boardHistory)
        {
            Location kingLocation = GetKingLocation(pieces, !isWhitesTurn);
            King king = (King)pieces[kingLocation.GetRank(), kingLocation.GetFile()];
            for (int i = 0; i < 8; i++)
            {
                Location possibleLocation = kingLocation.Clone();
                GetPossibleKingMoves(possibleLocation, i);
                Piece possibleLocationPiece = pieces[possibleLocation.GetRank(), possibleLocation.GetFile()];
                Move possibleMove = new Move(kingLocation, possibleLocation);
                if (possibleLocationPiece is EmptyPiece || possibleLocationPiece.IsWhite() != king.IsWhite())
                {
                    if (IsAnyPieceThreateningLocation(possibleMove.getEnding(), isWhitesTurn, boardHistory))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool CanAnyPieceMove(bool isWhitesTurn, Board[] boardHistory)
        {
            for (int i = 0; i < pieces.GetLength(0); i++)
            {
                for (int j = 0; j < pieces.GetLength(1); j++)
                {
                    Location startingLocation = new Location(i, j);
                    Piece piece = pieces[i, j];
                    if (piece.IsWhite() == isWhitesTurn)
                    {
                        for (int k = 0; k < pieces.GetLength(0); k++)
                        {
                            for (int l = 0; l < pieces.GetLength(1); l++)
                            {
                                Location potentialLocation = new Location(k, l);
                                Move move = new Move(startingLocation, potentialLocation);
                                if (piece.IsLegalMove(move, pieces, isWhitesTurn, boardHistory))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        private void GetPossibleKingMoves(Location kingLocation, int index)
        {
            switch (index)
            {
                case 0:
                    kingLocation.TraverseUp();
                    break;
                case 1:
                    kingLocation.TraverseDown();
                    break;
                case 2:
                    kingLocation.TraverseRight();
                    break;
                case 3:
                    kingLocation.TraverseLeft();
                    break;
                case 4:
                    kingLocation.TraverseUp();
                    kingLocation.TraverseRight();
                    break;
                case 5:
                    kingLocation.TraverseUp();
                    kingLocation.TraverseLeft();
                    break;
                case 6:
                    kingLocation.TraverseUp();
                    kingLocation.TraverseRight();
                    break;
                case 7:
                    kingLocation.TraverseUp();
                    kingLocation.TraverseLeft();
                    break;
            }
        }

        public bool GetIsInCheck(bool isWhitesTurn, Board[] boardHistory)
        {
            return IsAnyPieceThreateningLocation(GetKingLocation(pieces, !isWhitesTurn), !isWhitesTurn, boardHistory);
        }

        public void NextMove(Move move, bool isWhitesTurn, Board[] boardHistory)
        {
            this.pieces[move.getStarting().GetRank(), move.getStarting().GetFile()]
                .Move(move, this.pieces, boardHistory);
        }

        public bool TryMove(Move move, bool isWhitesTurn, Board[] boardHistory)
        {
            int rank = move.getStarting().GetRank();
            int file = move.getStarting().GetFile();
            return this.pieces[rank, file].IsLegalMove(move, this.pieces, isWhitesTurn, boardHistory) &&
                   !IsPieceSuspended(move, isWhitesTurn, boardHistory);
        }

        private Piece[,] ClonePieceArray()
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

        private bool IsAnyPieceThreateningLocation(Location location, bool isWhitesTurn, Board[] boardHistory)
        {
            for (int i = 0; i < pieces.GetLength(0); i++)
            {
                for (int j = 0; j < pieces.GetLength(1); j++)
                {
                    Move move = new Move(new Location(i, j), location);
                    Piece piece = pieces[i, j];
                    if (!(piece is EmptyPiece))
                    {
                        if (piece.IsWhite() != isWhitesTurn)
                        {
                            if (piece.IsLegalMove(move, pieces, !isWhitesTurn, boardHistory))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private Location GetKingLocation(Piece[,] pieces, bool isWhitesTurns)
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

        private bool IsPieceSuspended(Move move, bool isWhitesTurn, Board[] boardHistory)
        {
            Piece[,] preMovePieces = ClonePieceArray();
            if (IsMoveCastling(move))
            {
                return IsCastlingIllegal(move, isWhitesTurn, boardHistory);
            }

            NextMove(move, isWhitesTurn, boardHistory);
            bool isSuspended =
                IsAnyPieceThreateningLocation(GetKingLocation(pieces, isWhitesTurn), isWhitesTurn, boardHistory);
            pieces = preMovePieces;
            return isSuspended;
        }

        private bool IsCastlingIllegal(Move move, bool isWhitesTurn, Board[] boardHistory)
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
                    if (IsAnyPieceThreateningLocation(new Location(move.getStarting().GetRank(), i), isWhitesTurn,
                            boardHistory))
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

        private bool IsMoveCastling(Move move)
        {
            if ((move.getStarting().GetRank() == 0 || move.getStarting().GetRank() == 7) &&
                move.getStarting().GetFile() == 4 &&
                (move.getEnding().GetRank() == 0 || move.getEnding().GetRank() == 7) &&
                (move.getEnding().GetFile() == 6 || move.getEnding().GetFile() == 2))
            {
                Piece piece = pieces[move.getStarting().GetRank(), move.getStarting().GetFile()];
                if (piece is King)
                {
                    return true;
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
                    new Pawn(false), new Pawn(false), new Pawn(false), new Pawn(false),
                    new Pawn(false), new Pawn(false), new Pawn(false), new Pawn(false),
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

        protected Location Traverse(Move move)
        {
            Location changedLocation = move.getStarting().Clone();
            Location currentLocation = move.getStarting().Clone();
            if (move.getStarting().GetFile() == move.getEnding().GetFile() ||
                move.getStarting().GetRank() == move.getEnding().GetRank())
            {
                if (move.getStarting().GetFile() == move.getEnding().GetFile())
                {
                    if (move.getStarting().GetRank() > move.getEnding().GetRank())
                    {
                        currentLocation.TraverseUp();
                    }
                    else
                    {
                        currentLocation.TraverseDown();
                    }
                }
                else if (move.getStarting().GetFile() > move.getEnding().GetFile())
                {
                    currentLocation.TraverseLeft();
                }
                else
                {
                    currentLocation.TraverseRight();
                }

                return currentLocation;
            }

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

            if (changedLocation.GetRank() != currentLocation.GetRank() &&
                changedLocation.GetFile() != currentLocation.GetFile())
                return changedLocation;
            return currentLocation;
        }

        public virtual bool IsLegalMove(Move move, Piece[,] pieces, bool isWhitesTurn, Board[] boardHistory)
        {
            Piece startingPiece = pieces[move.getStarting().GetRank(), move.getStarting().GetFile()];
            Piece endingPiece = pieces[move.getEnding().GetRank(), move.getEnding().GetFile()];
            if (!(startingPiece is EmptyPiece))
            {
                if (startingPiece.IsWhite() == isWhitesTurn)
                {
                    if (!(endingPiece is EmptyPiece) && startingPiece.IsWhite() == endingPiece.IsWhite())
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        protected bool IsPieceBlockedIn(Move move, Piece[,] pieces)
        {
            Location currentLocation = Traverse(new Move(move.getStarting().Clone(), move.getEnding().Clone()));
            while (!currentLocation.Equals(move.getEnding()))
            {
                Piece currentPiece = pieces[currentLocation.GetRank(), currentLocation.GetFile()];
                if (!(currentPiece is EmptyPiece))
                {
                    return true;
                }

                currentLocation = Traverse(new Move(currentLocation, move.getEnding()));
            }

            return false;
        }

        public virtual void Move(Move move, Piece[,] pieces, Board[] boardHistory)
        {
            Piece currentPiece = pieces[move.getStarting().GetRank(), move.getStarting().GetFile()];
            pieces[move.getEnding().GetRank(), move.getEnding().GetFile()] = currentPiece.Clone();
            pieces[move.getStarting().GetRank(), move.getStarting().GetFile()] = new EmptyPiece();
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

        public override bool IsLegalMove(Move move, Piece[,] pieces, bool isWhitesTurn, Board[] boardHistory)
        {
            return base.IsLegalMove(move, pieces, isWhitesTurn, boardHistory) &&
                   IsMoveValidDirection(move, isWhitesTurn) && IsMoveTryingToEatOrEnpassant(move, pieces, boardHistory);
        }

        public bool IsMoveValidDirection(Move move, bool isWhitesTurn)
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

            if (locationDifference.GetRank() == 1 && Math.Abs(locationDifference.GetFile()) == 1 && isWhitesTurn ||
                locationDifference.GetRank() == -1 && Math.Abs(locationDifference.GetFile()) == 1 && !isWhitesTurn)
            {
                return true;
            }

            return false;
        }

        public bool IsMoveTryingToEatOrEnpassant(Move move, Piece[,] pieces, Board[] boardHistory)
        {
            Piece piece = pieces[move.getEnding().GetRank(), move.getEnding().GetFile()];
            if (piece is EmptyPiece)
            {

            }
            return true;
        }
        
        public override Piece Clone()
        {
            return new Pawn(isWhite, firstMove);
        }

        public override void Move(Move move, Piece[,] pieces, Board[] boardHistory)
        {
            this.firstMove = false;
            base.Move(move, pieces, boardHistory);
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

        public override bool IsLegalMove(Move move, Piece[,] pieces, bool isWhitesTurn, Board[] boardHistory)
        {
            return base.IsLegalMove(move, pieces, isWhitesTurn, boardHistory) && IsMoveValidDirection(move) &&
                   !IsPieceBlockedIn(move, pieces);
        }

        private bool IsMoveValidDirection(Move move)
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

        public override void Move(Move move, Piece[,] pieces, Board[] boardHistory)
        {
            this.firstMove = false;
            base.Move(move, pieces, boardHistory);
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

        public override bool IsLegalMove(Move move, Piece[,] pieces, bool isWhitesTurn, Board[] boardHistory)
        {
            return base.IsLegalMove(move, pieces, isWhitesTurn, boardHistory) && IsMoveValidDirection(move) &&
                   !IsPieceBlockedIn(move, pieces);
        }

        public bool IsMoveValidDirection(Move move)
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

        public override bool IsLegalMove(Move move, Piece[,] pieces, bool isWhitesTurn, Board[] boardHistory)
        {
            if (!base.IsLegalMove(move, pieces, isWhitesTurn, boardHistory))
            {
                return false;
            }

            Location locationDifference = move.GetLocationDifference();
            locationDifference.setRank(Math.Abs(locationDifference.GetRank()));
            locationDifference.setFile(Math.Abs(locationDifference.GetFile()));
            if ((locationDifference.GetRank() == 1 || locationDifference.GetRank() == 2) &&
                (locationDifference.GetFile() == 1 || locationDifference.GetFile() == 2))
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

        public override bool IsLegalMove(Move move, Piece[,] pieces, bool isWhitesTurn, Board[] boardHistory)
        {
            Rook rookAsQueen = new Rook(isWhite);
            Bishop bishopAsQueen = new Bishop(isWhite);
            return bishopAsQueen.IsLegalMove(move, pieces, isWhitesTurn, boardHistory) ||
                   rookAsQueen.IsLegalMove(move, pieces, isWhitesTurn, boardHistory);
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

        public override bool IsLegalMove(Move move, Piece[,] pieces, bool isWhitesTurn, Board[] boardHistory)
        {
            if (IsMoveCastling(move, pieces, isWhitesTurn))
            {
                if (!base.IsLegalMove(move, pieces, isWhitesTurn, boardHistory))
                {
                    return false;
                }

                return true;
            }

            return IsMoveValidDirection(move) && base.IsLegalMove(move, pieces, isWhitesTurn, boardHistory);
        }

        public bool IsFirstTurn()
        {
            return this.firstMove;
        }

        public override void Move(Move move, Piece[,] pieces, Board[] boardHistory)
        {
            if (IsMoveCastling(move, pieces, isWhite))
            {
                this.firstMove = false;
                int rookLocationIndex = move.getEnding().GetFile() > 4 ? 7 : 0;
                int rookNewLocationIndex = rookLocationIndex > 4 ? 5 : 3;
                Rook rook = (Rook)pieces[move.getStarting().GetRank(), rookLocationIndex];
                rook.SetFirstTurn(false);
                pieces[move.getEnding().GetRank(), move.getEnding().GetFile()] = Clone();
                pieces[move.getStarting().GetRank(), rookNewLocationIndex] = rook.Clone();
                pieces[move.getStarting().GetRank(), rookLocationIndex] = new EmptyPiece();
                pieces[move.getStarting().GetRank(), move.getStarting().GetFile()] = new EmptyPiece();
            }
            else
            {
                this.firstMove = false;
                base.Move(move, pieces, boardHistory);
            }
        }

        private bool IsMoveValidDirection(Move move)
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
}