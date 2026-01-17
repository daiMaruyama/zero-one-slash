using UnityEngine;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
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

    Task _initTask;
    bool _isReady;

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
            // Environmentを分けてる場合はここを使う（未使用なら消してOK）
            // var options = new InitializationOptions().SetEnvironmentName("production");
            // await UnityServices.InitializeAsync(options);

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
            Debug.LogError($"[Ranking] Init Error: {e.Message}");
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
        catch (RequestFailedException)
        {
            Debug.LogWarning("[Ranking] Login failed. Create new account...");
            PlayerPrefs.DeleteKey(KEY_USERNAME);
            PlayerPrefs.DeleteKey(KEY_PASSWORD);

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
            Debug.LogError($"[Ranking] Create User Failed: {e.Message}");
        }
    }

    string SanitizePlayerName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "Unknown";

        // スペース禁止（Unity仕様）なので全部除去
        string s = raw.Replace(" ", "").Replace("　", "").Replace("\n", "").Replace("\r", "").Replace("\t", "");

        // 何も残らなかったらUnknown
        if (string.IsNullOrEmpty(s)) s = "Unknown";

        // 長すぎると困るので適当に制限（任意）
        if (s.Length > 12) s = s.Substring(0, 12);

        return s;
    }

    public async Task SubmitScoreWithUpdateName(int score, string playerName)
    {
        await EnsureReadyAsync();

        if (!_isReady) return;
        if (!AuthenticationService.Instance.IsSignedIn) return;

        try
        {
            string validName = SanitizePlayerName(playerName);

            // PlayerName更新（LeaderboardEntry.PlayerName に乗る）
            // スペースがあると失敗する仕様あり :contentReference[oaicite:1]{index=1}
            await AuthenticationService.Instance.UpdatePlayerNameAsync(validName);

            await LeaderboardsService.Instance.AddPlayerScoreAsync(LEADERBOARD_ID, score);

            Debug.Log($"[Ranking] Score Submitted: {score} (Name: {validName})");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Ranking] Submit Score Error: {e.Message}");
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
            Debug.LogError($"[Ranking] Get Ranking Error: {e.Message}");
            return null;
        }
    }
}
