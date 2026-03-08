using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance;

    public FirebaseAuth Auth { get; private set; }
    public DatabaseReference DB { get; private set; }

    async void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);

        var dep = await FirebaseApp.CheckAndFixDependenciesAsync();

        if (dep != DependencyStatus.Available)
        {
            Debug.LogError("Firebase dependency error");
            return;
        }

        Auth = FirebaseAuth.DefaultInstance;
        DB = FirebaseDatabase.DefaultInstance.RootReference;

        if (Auth.CurrentUser == null)
            await Auth.SignInAnonymouslyAsync();

        Debug.Log("Firebase Ready");
    }
}