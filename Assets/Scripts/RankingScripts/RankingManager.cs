using UnityEngine;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using System.Collections.Generic;

public class RankingManager : MonoBehaviour
{
    public static RankingManager instance;

    const string LEADERBOARD_ID = "MyGameHighScore";
    const string KEY_USERNAME = "AUTO_USER_NAME";
    const string KEY_PASSWORD = "AUTO_USER_PASS";

    [Header("デモ用：同一端末で何回でも別枠登録したいならON")]
    [SerializeField] bool createNewAccountEverySubmit = true;

    Task _initTask;
    bool _isReady;

    public int LastSubmittedRank { get; private set; } = -1;   // 0が1位
    public double LastSubmittedScore { get; private set; } = -1;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // 初期化を開始（完了前に呼ばれても待てるようにする）
            _initTask = InitializeInternalAsync();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    async Task InitializeInternalAsync()
    {
        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AutoSignInAsync();
            }

            _isReady = AuthenticationService.Instance.IsSignedIn;
            Debug.Log($"[Ranking] Ready. PlayerID: {AuthenticationService.Instance.PlayerId}");
        }
        catch (System.Exception e)
        {
            _isReady = false;
            Debug.LogError($"[Ranking] Init Error: {e}");
        }
    }

    public async Task EnsureReadyAsync()
    {
        if (_initTask != null) await _initTask;
    }

    async Task AutoSignInAsync()
    {
        string username = PlayerPrefs.GetString(KEY_USERNAME, "");
        string password = PlayerPrefs.GetString(KEY_PASSWORD, "");

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            await CreateNewUserAndLogin();
            return;
        }

        try
        {
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
        }
        catch (Unity.Services.Core.RequestFailedException)
        {
            Debug.LogWarning("[Ranking] Login failed. Create new account...");
            PlayerPrefs.DeleteKey(KEY_USERNAME);
            PlayerPrefs.DeleteKey(KEY_PASSWORD);
            PlayerPrefs.Save();

            await CreateNewUserAndLogin();
        }
    }

    async Task CreateNewUserAndLogin()
    {
        // Username/Password はプロトタイプならOK（PlayerPrefs保存は本来は非推奨）
        string newName = "Player" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
        string newPass = "Pass!" + System.Guid.NewGuid().ToString("N").Substring(0, 12);

        try
        {
            await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(newName, newPass);

            // 次回起動時に自動ログインできるように保存（createNewAccountEverySubmitでも害はない）
            PlayerPrefs.SetString(KEY_USERNAME, newName);
            PlayerPrefs.SetString(KEY_PASSWORD, newPass);
            PlayerPrefs.Save();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(newName, newPass);
            }

            Debug.Log($"[Ranking] New Account Created: {newName}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Ranking] Create User Failed: {e}");
        }
    }

    async Task ForceNewAccountAsync()
    {
        // 送信ごとに別PlayerIDにしたいので、現在のログインを捨てて新規アカウントを作る
        try
        {
            if (AuthenticationService.Instance.IsSignedIn)
                AuthenticationService.Instance.SignOut();
        }
        catch { }

        // 次回起動時に古いアカウントへ戻らないように消す
        PlayerPrefs.DeleteKey(KEY_USERNAME);
        PlayerPrefs.DeleteKey(KEY_PASSWORD);
        PlayerPrefs.Save();

        await CreateNewUserAndLogin();

        _isReady = AuthenticationService.Instance.IsSignedIn;
        if (_isReady)
            Debug.Log($"[Ranking] Switched Account. PlayerID: {AuthenticationService.Instance.PlayerId}");
        else
            Debug.LogWarning("[Ranking] Switch Account failed.");
    }

    public async Task<bool> ShouldOpenNameInputAsync(int score, int topN)
    {
        await EnsureReadyAsync();
        if (!_isReady || !AuthenticationService.Instance.IsSignedIn) return false;

        // B案（何回でも別枠登録）では「自分のベスト」判定は不要。
        // TopNに食い込めるなら入力を出す。埋まってなければ無条件でOK。
        try
        {
            var page = await LeaderboardsService.Instance.GetScoresAsync(
                LEADERBOARD_ID,
                new GetScoresOptions { Limit = topN }
            );

            var list = page.Results;

            if (list == null || list.Count < topN) return true;

            double nthScore = list[list.Count - 1].Score;
            return score >= nthScore;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Ranking] ShouldOpenNameInput Error: {e.Message}");
            return false;
        }
    }

    string SanitizePlayerName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "Unknown";

        // スペース禁止があるので全部除去
        string s = raw.Replace(" ", "")
                      .Replace("　", "")
                      .Replace("\n", "")
                      .Replace("\r", "")
                      .Replace("\t", "");

        if (string.IsNullOrEmpty(s)) s = "Unknown";
        if (s.Length > 12) s = s.Substring(0, 12);

        return s;
    }

    public async Task SubmitScoreWithUpdateName(int score, string playerName)
    {
        await EnsureReadyAsync();
        if (!_isReady) return;

        // 送信のたびに新しいPlayerIDを作る
        if (createNewAccountEverySubmit)
        {
            await ForceNewAccountAsync();
            if (!_isReady || !AuthenticationService.Instance.IsSignedIn) return;
        }
        else
        {
            if (!AuthenticationService.Instance.IsSignedIn) return;
        }

        try
        {
            string validName = SanitizePlayerName(playerName);

            // PlayerName更新（このアカウントの表示名）
            await AuthenticationService.Instance.UpdatePlayerNameAsync(validName);

            // スコア送信（このPlayerIDの行が作られる）
            await LeaderboardsService.Instance.AddPlayerScoreAsync(LEADERBOARD_ID, score);

            Debug.Log($"[Ranking] Score Submitted: {score} (Name: {validName})");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Ranking] Submit Score Error: {e}");
        }

        // 送信直後の順位を取ってリザルト表示に使う
        try
        {
            var me = await LeaderboardsService.Instance.GetPlayerScoreAsync(LEADERBOARD_ID);
            LastSubmittedRank = me.Rank;
            LastSubmittedScore = me.Score;
        }
        catch
        {
            LastSubmittedRank = -1;
            LastSubmittedScore = -1;
        }
    }

    public async Task<List<LeaderboardEntry>> GetRanking(int limit = 10)
    {
        await EnsureReadyAsync();

        if (!_isReady) return null;
        if (!AuthenticationService.Instance.IsSignedIn) return null;

        try
        {
            var response = await LeaderboardsService.Instance.GetScoresAsync(
                LEADERBOARD_ID,
                new GetScoresOptions { Limit = limit }
            );

            return response.Results;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Ranking] Get Ranking Error: {e}");
            return null;
        }
    }
}
