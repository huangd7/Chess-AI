using System.Collections.Generic;
using Chessticle;

public class ChessAI
{
    const int CheckmateScore = 1000000;

    int m_LastBestStart = -1;
    int m_LastBestTarget = -1;

    public (int start, int target) GetBestMove(Chessboard board)
    {
        int depth = GetSearchDepth(board);
        var aiColor = board.CurrentPlayer;
        var moves = GetOrderedMoves(board, aiColor);

        if (moves.Count == 0)
            return (-1, -1);

        bool maximizing = aiColor == Color.White;
        int bestScore = maximizing ? int.MinValue : int.MaxValue;
        (int start, int target) bestMove = moves[0];

        foreach (var move in moves)
        {
            if (!board.TryMove(move.start, move.target, Piece.None, out MoveResult result))
                continue;

            int score;

            if (result == MoveResult.WhiteCheckmated)
            {
                score = -CheckmateScore;
            }
            else if (result == MoveResult.BlackCheckmated)
            {
                score = CheckmateScore;
            }
            else if (result == MoveResult.StaleMate)
            {
                score = GetDrawScore(board);
            }
            else
            {
                score = Minimax(board, depth - 1, int.MinValue, int.MaxValue);
            }

            board.UndoLastMove();

            score += GetRootMoveBonus(move, aiColor);

            if (maximizing)
            {
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
            }
            else
            {
                if (score < bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
            }
        }

        m_LastBestStart = bestMove.start;
        m_LastBestTarget = bestMove.target;
        return bestMove;
    }

    int Minimax(Chessboard board, int depth, int alpha, int beta)
    {
        if (depth == 0)
            return Evaluation.Evaluate(board);

        var moves = GetOrderedMoves(board, board.CurrentPlayer);

        if (moves.Count == 0)
            return Evaluation.Evaluate(board);

        bool maximizing = board.CurrentPlayer == Color.White;

        if (maximizing)
        {
            int bestEval = int.MinValue;

            foreach (var move in moves)
            {
                if (!board.TryMove(move.start, move.target, Piece.None, out MoveResult result))
                    continue;

                int eval;

                if (result == MoveResult.WhiteCheckmated)
                {
                    eval = -CheckmateScore;
                }
                else if (result == MoveResult.BlackCheckmated)
                {
                    eval = CheckmateScore;
                }
                else if (result == MoveResult.StaleMate)
                {
                    eval = GetDrawScore(board);
                }
                else
                {
                    eval = Minimax(board, depth - 1, alpha, beta);
                }

                board.UndoLastMove();

                if (eval > bestEval)
                    bestEval = eval;

                if (eval > alpha)
                    alpha = eval;

                if (beta <= alpha)
                    break;
            }

            return bestEval;
        }
        else
        {
            int bestEval = int.MaxValue;

            foreach (var move in moves)
            {
                if (!board.TryMove(move.start, move.target, Piece.None, out MoveResult result))
                    continue;

                int eval;

                if (result == MoveResult.WhiteCheckmated)
                {
                    eval = -CheckmateScore;
                }
                else if (result == MoveResult.BlackCheckmated)
                {
                    eval = CheckmateScore;
                }
                else if (result == MoveResult.StaleMate)
                {
                    eval = GetDrawScore(board);
                }
                else
                {
                    eval = Minimax(board, depth - 1, alpha, beta);
                }

                board.UndoLastMove();

                if (eval < bestEval)
                    bestEval = eval;

                if (eval < beta)
                    beta = eval;

                if (beta <= alpha)
                    break;
            }

            return bestEval;
        }
    }

    int GetDrawScore(Chessboard board)
    {
        int eval = Evaluation.Evaluate(board);

        if (eval >= 300)
            return -80;
        if (eval <= -300)
            return 80;

        return 0;
    }

    int GetRootMoveBonus((int start, int target) move, Color aiColor)
    {
        int bonus = 0;

        if (move.start == m_LastBestTarget && move.target == m_LastBestStart)
            bonus -= 40;

        return aiColor == Color.White ? bonus : -bonus;
    }

    int GetSearchDepth(Chessboard board)
    {
        int pieceCount = CountPieces(board);

        if (pieceCount > 22)
            return 2;
        return 3;
    }

    int CountPieces(Chessboard board)
    {
        int count = 0;

        for (int i = 0; i < 64; i++)
        {
            int idx = Chessboard.IndexToIndex0X88(i);
            var (piece, _) = board.GetPiece(idx);

            if (piece != Piece.None)
                count++;
        }

        return count;
    }

    List<(int start, int target)> GetOrderedMoves(Chessboard board, Color color)
    {
        var scoredMoves = new List<((int start, int target) move, int score)>();

        for (int i = 0; i < 64; i++)
        {
            int start = Chessboard.IndexToIndex0X88(i);
            var (piece, pieceColor) = board.GetPiece(start);

            if (piece == Piece.None || pieceColor != color)
                continue;

            for (int j = 0; j < 64; j++)
            {
                int target = Chessboard.IndexToIndex0X88(j);
                var (capturedPiece, capturedColor) = board.GetPiece(target);

                if (!board.TryMove(start, target, Piece.None, out MoveResult result))
                    continue;

                int moveScore = 0;

                if (capturedPiece != Piece.None && capturedColor != pieceColor)
                {
                    moveScore += 10 * Evaluation.GetPieceValueForAI(capturedPiece)
                               - Evaluation.GetPieceValueForAI(piece);
                }

                if (result == MoveResult.WhiteCheckmated || result == MoveResult.BlackCheckmated)
                {
                    moveScore += 100000;
                }

                moveScore += Evaluation.GetMoveBonus(board, target);
                board.UndoLastMove();

                scoredMoves.Add(((start, target), moveScore));
            }
        }

        scoredMoves.Sort((a, b) => b.score.CompareTo(a.score));

        var moves = new List<(int start, int target)>();
        foreach (var entry in scoredMoves)
            moves.Add(entry.move);

        return moves;
    }
}