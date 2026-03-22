using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace Chessticle
{
    public class GameManager : MonoBehaviour
    {
        const int k_StartClockTime = 10 * 60;

        ChessboardUI m_ChessboardUI;
        readonly Clock m_Clock = new(k_StartClockTime);

        ChessAI ai = new ChessAI();

        public static Color selectedPlayerColor = Color.White;
        public Color playerColor;

        MoveResult m_LastMoveResult;
        bool m_GameOver;
        bool m_WhiteFlagged;
        bool m_BlackFlagged;
        bool m_AiThinking;

        void Awake()
        {
            Assert.AreEqual(1, FindObjectsOfType<GameManager>().Length);

            m_ChessboardUI = FindObjectOfType<ChessboardUI>();

            m_ChessboardUI.LocalPlayerMoved += OnLocalMoveFinished;
            m_ChessboardUI.ResignationRequested += OnResignRequested;
            m_ChessboardUI.NewOpponentRequested += RestartGame;
            m_ChessboardUI.ClaimDrawRequested += OnClaimDrawRequested;
            m_ChessboardUI.OfferDrawRequested += OnOfferDrawRequested;
        }

        void OnDestroy()
        {
            if (m_ChessboardUI == null) return;

            m_ChessboardUI.LocalPlayerMoved -= OnLocalMoveFinished;
            m_ChessboardUI.ResignationRequested -= OnResignRequested;
            m_ChessboardUI.NewOpponentRequested -= RestartGame;
            m_ChessboardUI.ClaimDrawRequested -= OnClaimDrawRequested;
            m_ChessboardUI.OfferDrawRequested -= OnOfferDrawRequested;
        }

        void Start()
        {
            playerColor = selectedPlayerColor;
            m_ChessboardUI.StartGame(playerColor);

            m_Clock.Stop();
            m_Clock.SwitchPlayer(0);

            m_ChessboardUI.ShowTime(Color.White, k_StartClockTime);
            m_ChessboardUI.ShowTime(Color.Black, k_StartClockTime);
            m_ChessboardUI.ShowCurrentPlayerIndicator(Color.White);
            m_ChessboardUI.SetResignButtonActive(true);
            m_ChessboardUI.SetNewOpponentButtonActive(false);
            m_ChessboardUI.HideLoadingIndicator();
            m_ChessboardUI.HideMessage();
            m_ChessboardUI.ShowOfferDrawButton();
            m_ChessboardUI.HideAcceptDrawButton();
            m_ChessboardUI.RefreshClaimDrawButton();

            if (playerColor == Color.Black)
            {
                StartCoroutine(AIMove());
            }
        }

        public void TogglePlayerColor()
        {
            if (selectedPlayerColor == Color.White)
                selectedPlayerColor = Color.Black;
            else
                selectedPlayerColor = Color.White;

            Debug.Log("Player is now: " + selectedPlayerColor);

            RestartGame();
        }

        void Update()
        {
            if (m_GameOver) return;

            float whiteTime = m_Clock.GetTime(Color.White, Time.timeAsDouble);
            float blackTime = m_Clock.GetTime(Color.Black, Time.timeAsDouble);

            m_ChessboardUI.ShowTime(Color.White, whiteTime);
            m_ChessboardUI.ShowTime(Color.Black, blackTime);

            if (whiteTime <= 0 && !m_WhiteFlagged)
            {
                m_WhiteFlagged = true;
                EndGame("Black won on time.");
            }
            else if (blackTime <= 0 && !m_BlackFlagged)
            {
                m_BlackFlagged = true;
                EndGame("White won on time.");
            }
        }

        void OnLocalMoveFinished(int startIdx, int targetIdx, Piece promotionPiece)
        {
            if (m_GameOver) return;

            m_Clock.SwitchPlayer(Time.timeAsDouble);

            var nextPlayer = m_ChessboardUI.CurrentPlayer;
            m_ChessboardUI.ShowCurrentPlayerIndicator(nextPlayer);
            m_ChessboardUI.RefreshClaimDrawButton();

            m_LastMoveResult = m_ChessboardUI.LastMoveResult;

            switch (m_LastMoveResult)
            {
                case MoveResult.WhiteCheckmated:
                    EndGame("Black won by checkmate.");
                    return;

                case MoveResult.BlackCheckmated:
                    EndGame("White won by checkmate.");
                    return;

                case MoveResult.StaleMate:
                    EndGame("Stalemate.");
                    return;
            }

            if (m_ChessboardUI.CurrentPlayer != playerColor && !m_AiThinking)
            {
                StartCoroutine(AIMove());
            }
        }

        void OnResignRequested()
        {
            if (m_GameOver) return;

            if (m_ChessboardUI.CurrentPlayer == Color.White)
            {
                EndGame("Black won by resignation.");
            }
            else
            {
                EndGame("White won by resignation.");
            }
        }

        void OnClaimDrawRequested()
        {
            if (m_GameOver) return;
            EndGame("Draw.");
        }

        void OnOfferDrawRequested()
        {
            if (m_GameOver) return;

            // In hotseat, treat "offer draw" as immediate agreed draw.
            EndGame("Draw.");
        }

        void EndGame(string message)
        {
            m_GameOver = true;
            m_Clock.Stop();
            m_ChessboardUI.ShowMessage(message);
            m_ChessboardUI.StopGame();
        }

        static void RestartGame()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        System.Collections.IEnumerator AIMove()
        {
            if (m_GameOver) yield break;

            m_AiThinking = true;
            m_ChessboardUI.ShowLoadingIndicator();

            yield return new WaitForSeconds(0.3f);

            var board = m_ChessboardUI.Chessboard;
            var move = ai.GetBestMove(board);

            bool didMove = m_ChessboardUI.ApplyExternalMove(move.start, move.target, Piece.None);

            m_ChessboardUI.HideLoadingIndicator();
            m_AiThinking = false;

            if (!didMove)
            {
                yield break;
            }

            m_Clock.SwitchPlayer(Time.timeAsDouble);

            var nextPlayer = m_ChessboardUI.CurrentPlayer;
            m_ChessboardUI.ShowCurrentPlayerIndicator(nextPlayer);
            m_ChessboardUI.RefreshClaimDrawButton();

            m_LastMoveResult = m_ChessboardUI.LastMoveResult;

            switch (m_LastMoveResult)
            {
                case MoveResult.WhiteCheckmated:
                    EndGame("Black won by checkmate.");
                    yield break;

                case MoveResult.BlackCheckmated:
                    EndGame("White won by checkmate.");
                    yield break;

                case MoveResult.StaleMate:
                    EndGame("Stalemate.");
                    yield break;
            }
        }
    }
}