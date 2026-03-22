using Chessticle;

public static class Evaluation
{
    static readonly int[] PawnTable =
    {
         0,  0,  0,  0,  0,  0,  0,  0,
         5, 10, 10,-10,-10, 10, 10,  5,
         5, -5,-10,  0,  0,-10, -5,  5,
         0,  0,  0, 20, 20,  0,  0,  0,
         5,  5, 10, 25, 25, 10,  5,  5,
        10, 10, 20, 30, 30, 20, 10, 10,
        50, 50, 50, 50, 50, 50, 50, 50,
         0,  0,  0,  0,  0,  0,  0,  0
    };

    static readonly int[] KnightTable =
    {
        -50,-40,-30,-30,-30,-30,-40,-50,
        -40,-20,  0,  5,  5,  0,-20,-40,
        -30,  5, 10, 15, 15, 10,  5,-30,
        -30,  0, 15, 20, 20, 15,  0,-30,
        -30,  5, 15, 20, 20, 15,  5,-30,
        -30,  0, 10, 15, 15, 10,  0,-30,
        -40,-20,  0,  0,  0,  0,-20,-40,
        -50,-40,-30,-30,-30,-30,-40,-50
    };

    static readonly int[] BishopTable =
    {
        -20,-10,-10,-10,-10,-10,-10,-20,
        -10,  5,  0,  0,  0,  0,  5,-10,
        -10, 10, 10, 10, 10, 10, 10,-10,
        -10,  0, 10, 10, 10, 10,  0,-10,
        -10,  5,  5, 10, 10,  5,  5,-10,
        -10,  0,  5, 10, 10,  5,  0,-10,
        -10,  0,  0,  0,  0,  0,  0,-10,
        -20,-10,-10,-10,-10,-10,-10,-20
    };

    static readonly int[] RookTable =
    {
         0,  0,  5, 10, 10,  5,  0,  0,
        -5,  0,  0,  0,  0,  0,  0, -5,
        -5,  0,  0,  0,  0,  0,  0, -5,
        -5,  0,  0,  0,  0,  0,  0, -5,
        -5,  0,  0,  0,  0,  0,  0, -5,
        -5,  0,  0,  0,  0,  0,  0, -5,
         5, 10, 10, 10, 10, 10, 10,  5,
         0,  0,  0,  5,  5,  0,  0,  0
    };

    static readonly int[] QueenTable =
    {
        -20,-10,-10, -5, -5,-10,-10,-20,
        -10,  0,  0,  0,  0,  5,  0,-10,
        -10,  0,  5,  5,  5,  5,  5,-10,
         -5,  0,  5,  5,  5,  5,  0, -5,
          0,  0,  5,  5,  5,  5,  0, -5,
        -10,  5,  5,  5,  5,  5,  0,-10,
        -10,  0,  5,  0,  0,  0,  0,-10,
        -20,-10,-10, -5, -5,-10,-10,-20
    };

    static readonly int[] KingOpeningTable =
    {
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -20,-30,-30,-40,-40,-30,-30,-20,
        -10,-20,-20,-20,-20,-20,-20,-10,
         20, 20,  0,  0,  0,  0, 20, 20,
         20, 30, 10,  0,  0, 10, 30, 20
    };

    static readonly int[] KingEndgameTable =
    {
        -50,-30,-20,-20,-20,-20,-30,-50,
        -30,-10,  0,  0,  0,  0,-10,-30,
        -20,  0, 10, 15, 15, 10,  0,-20,
        -20,  0, 15, 20, 20, 15,  0,-20,
        -20,  0, 15, 20, 20, 15,  0,-20,
        -20,  0, 10, 15, 15, 10,  0,-20,
        -30,-10,  0,  0,  0,  0,-10,-30,
        -50,-30,-20,-20,-20,-20,-30,-50
    };

    public static int Evaluate(Chessboard board)
    {
        int score = 0;

        int[] whitePawnFiles = new int[8];
        int[] blackPawnFiles = new int[8];

        int whiteBishops = 0;
        int blackBishops = 0;

        int whiteNonPawnMaterial = 0;
        int blackNonPawnMaterial = 0;

        for (int i = 0; i < 64; i++)
        {
            int idx = Chessboard.IndexToIndex0X88(i);
            var (piece, color) = board.GetPiece(idx);

            if (piece == Piece.None)
                continue;

            var (rank, file) = Chessboard.Index0X88ToCoords(idx);

            if (piece == Piece.WhitePawn)
                whitePawnFiles[file]++;
            else if (piece == Piece.BlackPawn)
                blackPawnFiles[file]++;

            if (piece == Piece.Bishop)
            {
                if (color == Color.White) whiteBishops++;
                else blackBishops++;
            }

            if (piece != Piece.WhitePawn && piece != Piece.BlackPawn && piece != Piece.King)
            {
                if (color == Color.White) whiteNonPawnMaterial += GetPieceValueForAI(piece);
                else blackNonPawnMaterial += GetPieceValueForAI(piece);
            }
        }

        bool endgame = IsEndgame(whiteNonPawnMaterial, blackNonPawnMaterial);

        for (int i = 0; i < 64; i++)
        {
            int idx = Chessboard.IndexToIndex0X88(i);
            var (piece, color) = board.GetPiece(idx);

            if (piece == Piece.None)
                continue;

            int total =
                GetPieceValueForAI(piece) +
                GetPieceSquareBonus(piece, color, idx, endgame) +
                GetDevelopmentBonus(board, piece, color, idx) +
                GetKingSafetyBonus(piece, color, idx, endgame) +
                GetPawnStructureBonus(board, piece, color, idx, whitePawnFiles, blackPawnFiles) +
                GetRookFileBonus(board, piece, color, idx, whitePawnFiles, blackPawnFiles);

            if (color == Color.White)
                score += total;
            else
                score -= total;
        }

        if (whiteBishops >= 2) score += 30;
        if (blackBishops >= 2) score -= 30;

        return score;
    }

    public static int GetPieceValueForAI(Piece piece)
    {
        switch (piece)
        {
            case Piece.WhitePawn:
            case Piece.BlackPawn: return 100;
            case Piece.Knight: return 320;
            case Piece.Bishop: return 330;
            case Piece.Rook: return 500;
            case Piece.Queen: return 900;
            case Piece.King: return 20000;
            default: return 0;
        }
    }

    public static int GetMoveBonus(Chessboard board, int targetIdx)
    {
        var (piece, color) = board.GetPiece(targetIdx);

        if (piece == Piece.None)
            return 0;

        int bonus = 0;
        var (rank, file) = Chessboard.Index0X88ToCoords(targetIdx);

        bonus += GetPieceSquareBonus(piece, color, targetIdx, false) / 2;

        // centralization helps move ordering
        if (file >= 2 && file <= 5 && rank >= 2 && rank <= 5)
            bonus += 8;

        // encourage central pawn pushes
        if (piece == Piece.WhitePawn || piece == Piece.BlackPawn)
        {
            if (file == 3 || file == 4)
                bonus += 15;

            int advance = color == Color.White ? (6 - rank) : (rank - 1);
            bonus += advance * 2;
        }

        // prefer developing minors
        if (piece == Piece.Knight || piece == Piece.Bishop)
            bonus += 10;

        return bonus;
    }

    static int GetPieceSquareBonus(Piece piece, Color color, int idx0x88, bool endgame)
    {
        var (rank, file) = Chessboard.Index0X88ToCoords(idx0x88);
        int index = rank * 8 + file;

        if (color == Color.Black)
            index = MirrorIndex(index);

        switch (piece)
        {
            case Piece.WhitePawn:
            case Piece.BlackPawn: return PawnTable[index];
            case Piece.Knight: return KnightTable[index];
            case Piece.Bishop: return BishopTable[index];
            case Piece.Rook: return RookTable[index];
            case Piece.Queen: return QueenTable[index];
            case Piece.King: return endgame ? KingEndgameTable[index] : KingOpeningTable[index];
            default: return 0;
        }
    }

    static int GetDevelopmentBonus(Chessboard board, Piece piece, Color color, int idx0x88)
    {
        int bonus = 0;
        var (rank, file) = Chessboard.Index0X88ToCoords(idx0x88);
        int developedMinorPieces = CountDevelopedMinorPieces(board, color);

        // central pawns
        if (piece == Piece.WhitePawn || piece == Piece.BlackPawn)
        {
            bool isDPawn = file == 3;
            bool isEPawn = file == 4;

            if (isDPawn || isEPawn)
            {
                int advance = color == Color.White ? (6 - rank) : (rank - 1);

                if (advance == 1) bonus += 18;
                else if (advance == 2) bonus += 28;
                else if (advance >= 3) bonus += 20;
            }
        }

        // knights off back rank
        if (piece == Piece.Knight)
        {
            bool onStart =
                (color == Color.White && rank == 7 && (file == 1 || file == 6)) ||
                (color == Color.Black && rank == 0 && (file == 1 || file == 6));

            if (!onStart) bonus += 18;
        }

        // bishops off back rank
        if (piece == Piece.Bishop)
        {
            bool onStart =
                (color == Color.White && rank == 7 && (file == 2 || file == 5)) ||
                (color == Color.Black && rank == 0 && (file == 2 || file == 5));

            if (!onStart) bonus += 16;
        }

        // queen out too early
        if (piece == Piece.Queen)
        {
            bool queenOnStart =
                (color == Color.White && rank == 7 && file == 3) ||
                (color == Color.Black && rank == 0 && file == 3);

            if (!queenOnStart && developedMinorPieces < 2)
                bonus -= 35;
            else if (!queenOnStart && developedMinorPieces < 4)
                bonus -= 15;
        }

        // rook out too early
        if (piece == Piece.Rook)
        {
            bool rookOnStart =
                (color == Color.White && rank == 7 && (file == 0 || file == 7)) ||
                (color == Color.Black && rank == 0 && (file == 0 || file == 7));

            if (!rookOnStart && developedMinorPieces < 4)
                bonus -= 12;
        }

        return bonus;
    }

    static int GetKingSafetyBonus(Piece piece, Color color, int idx0x88, bool endgame)
    {
        if (piece != Piece.King)
            return 0;

        if (endgame)
            return 0;

        var (rank, file) = Chessboard.Index0X88ToCoords(idx0x88);

        // castled king
        if (color == Color.White && rank == 7 && (file == 6 || file == 2))
            return 45;

        if (color == Color.Black && rank == 0 && (file == 6 || file == 2))
            return 45;

        // king stuck in center
        if (color == Color.White && rank == 7 && file == 4)
            return -15;

        if (color == Color.Black && rank == 0 && file == 4)
            return -15;

        return 0;
    }

    static int GetPawnStructureBonus(
        Chessboard board,
        Piece piece,
        Color color,
        int idx0x88,
        int[] whitePawnFiles,
        int[] blackPawnFiles)
    {
        if (piece != Piece.WhitePawn && piece != Piece.BlackPawn)
            return 0;

        int bonus = 0;
        var (rank, file) = Chessboard.Index0X88ToCoords(idx0x88);

        int[] myFiles = color == Color.White ? whitePawnFiles : blackPawnFiles;
        int[] enemyFiles = color == Color.White ? blackPawnFiles : whitePawnFiles;

        // doubled pawns
        if (myFiles[file] > 1)
            bonus -= 14 * (myFiles[file] - 1);

        // isolated pawns
        bool leftFriendly = file > 0 && myFiles[file - 1] > 0;
        bool rightFriendly = file < 7 && myFiles[file + 1] > 0;
        if (!leftFriendly && !rightFriendly)
            bonus -= 12;

        // passed pawns
        bool passed = true;
        for (int f = file - 1; f <= file + 1; f++)
        {
            if (f < 0 || f > 7) continue;

            for (int r = 0; r < 8; r++)
            {
                int idx = Chessboard.CoordsToIndex0X88(r, f);
                var (otherPiece, otherColor) = board.GetPiece(idx);

                bool enemyPawn =
                    (otherPiece == Piece.WhitePawn || otherPiece == Piece.BlackPawn) &&
                    otherColor != color;

                if (!enemyPawn) continue;

                if (color == Color.White && r < rank)
                    passed = false;

                if (color == Color.Black && r > rank)
                    passed = false;
            }
        }

        if (passed)
        {
            int advance = color == Color.White ? (6 - rank) : (rank - 1);
            bonus += 12 + advance * 8;
        }

        // connected pawns
        if (leftFriendly || rightFriendly)
            bonus += 6;

        return bonus;
    }

    static int GetRookFileBonus(
        Chessboard board,
        Piece piece,
        Color color,
        int idx0x88,
        int[] whitePawnFiles,
        int[] blackPawnFiles)
    {
        if (piece != Piece.Rook)
            return 0;

        var (_, file) = Chessboard.Index0X88ToCoords(idx0x88);

        bool friendlyPawnOnFile = color == Color.White ? whitePawnFiles[file] > 0 : blackPawnFiles[file] > 0;
        bool enemyPawnOnFile = color == Color.White ? blackPawnFiles[file] > 0 : whitePawnFiles[file] > 0;

        if (!friendlyPawnOnFile && !enemyPawnOnFile)
            return 22; // open file

        if (!friendlyPawnOnFile && enemyPawnOnFile)
            return 12; // semi-open file

        return 0;
    }

    static int CountDevelopedMinorPieces(Chessboard board, Color color)
    {
        int count = 0;

        for (int i = 0; i < 64; i++)
        {
            int idx = Chessboard.IndexToIndex0X88(i);
            var (piece, pieceColor) = board.GetPiece(idx);

            if (pieceColor != color)
                continue;

            var (rank, file) = Chessboard.Index0X88ToCoords(idx);

            if (piece == Piece.Knight)
            {
                bool onStart =
                    (color == Color.White && rank == 7 && (file == 1 || file == 6)) ||
                    (color == Color.Black && rank == 0 && (file == 1 || file == 6));

                if (!onStart) count++;
            }

            if (piece == Piece.Bishop)
            {
                bool onStart =
                    (color == Color.White && rank == 7 && (file == 2 || file == 5)) ||
                    (color == Color.Black && rank == 0 && (file == 2 || file == 5));

                if (!onStart) count++;
            }
        }

        return count;
    }

    static bool IsEndgame(int whiteNonPawnMaterial, int blackNonPawnMaterial)
    {
        return whiteNonPawnMaterial <= 1300 && blackNonPawnMaterial <= 1300;
    }

    static int MirrorIndex(int index)
    {
        int rank = index / 8;
        int file = index % 8;
        int mirroredRank = 7 - rank;
        return mirroredRank * 8 + file;
    }
}