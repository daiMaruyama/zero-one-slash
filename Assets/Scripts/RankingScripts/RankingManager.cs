using UnityEngine;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards;
using System.Collections.Generic;

/// <summary>
/// Unity Gaming Services (UGS) を使用したクラウドランキング管理クラス
/// 匿名認証（Username/Password方式）を自動で行い、スコアの送信と取得を行う
/// </summary>
public class RankingManager : MonoBehaviour
{
    public static RankingManager instance;

    const string LEADERBOARD_ID = "MyGameHighScore";
    const string KEY_USERNAME = "AUTO_USER_NAME";
    const string KEY_PASSWORD = "AUTO_USER_PASS";

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            _ = InitializeAsync();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// UGSの初期化と自動サインインを実行
    /// </summary>
    async Task InitializeAsync()
    {
        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AutoSignInAsync();
            }

            Debug.Log($"[Ranking] Initialization Complete. PlayerID: {AuthenticationService.Instance.PlayerId}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Ranking] Initialization Error: {e.Message}");
        }
    }

    /// <summary>
    /// 自動ログイン処理
    /// </summary>
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
            // 認証失敗時は古い情報を破棄して作り直す
            Debug.LogWarning("[Ranking] Login failed with current credentials. Creating new user...");
            PlayerPrefs.DeleteKey(KEY_USERNAME);
            PlayerPrefs.DeleteKey(KEY_PASSWORD);

            await CreateNewUserAndLogin();
        }
    }

    /// <summary>
    /// 新規ユーザーを作成し、ログインして保存する
    /// </summary>
    async Task CreateNewUserAndLogin()
    {
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

    /// <summary>
    /// スコアを送信する
    /// </summary>
    public async Task SubmitScoreWithUpdateName(int score, string playerName)
    {
        if (!AuthenticationService.Instance.IsSignedIn) return;

        try
        {
            string validName = string.IsNullOrWhiteSpace(playerName) ? "Unknown" : playerName;
            await AuthenticationService.Instance.UpdatePlayerNameAsync(validName);
            await LeaderboardsService.Instance.AddPlayerScoreAsync(LEADERBOARD_ID, score);

            Debug.Log($"[Ranking] Score Submitted: {score} (Name: {validName})");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Ranking] Submit Score Error: {e.Message}");
        }
    }

    /// <summary>
    /// ランキングデータを取得する
    /// </summary>
    public async Task<List<Unity.Services.Leaderboards.Models.LeaderboardEntry>> GetRanking(int limit = 5)
    {
        if (!AuthenticationService.Instance.IsSignedIn) return null;

        try
        {
            var response = await LeaderboardsService.Instance.GetScoresAsync(LEADERBOARD_ID, new GetScoresOptions { Limit = limit });
            return response.Results;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Ranking] Get Ranking Error: {e.Message}");
            return null;
        }
    }
}