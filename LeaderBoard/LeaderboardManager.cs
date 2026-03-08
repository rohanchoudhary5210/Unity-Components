using Firebase.Database;
using System.Collections.Generic;
using UnityEngine;

public class LeaderboardManager : MonoBehaviour
{
    public GameObject rowPrefab;
    public Transform content;

    List<PlayerBox> rowPool = new List<PlayerBox>();

    void Start()
    {
        var reference = FirebaseDatabase.DefaultInstance
            .GetReference("users")
            .OrderByChild("score")
            .LimitToLast(10);

        reference.ValueChanged += OnLeaderboardUpdated;
    }

    void OnLeaderboardUpdated(object sender, ValueChangedEventArgs e)
    {
        if (e.DatabaseError != null) return;

        List<DataSnapshot> players = new List<DataSnapshot>();

        foreach (var snap in e.Snapshot.Children)
            players.Add(snap);

        players.Reverse();

        EnsurePoolSize(players.Count);

        for (int i = 0; i < rowPool.Count; i++)
        {
            if (i < players.Count)
            {
                var snap = players[i];

                string username = snap.Child("username").Value.ToString();
                int score = int.Parse(snap.Child("score").Value.ToString());

                rowPool[i].gameObject.SetActive(true);
                rowPool[i].SetData(username, score, i + 1);
            }
            else
            {
                rowPool[i].gameObject.SetActive(false);
            }
        }
    }

    void EnsurePoolSize(int needed)
    {
        while (rowPool.Count < needed)
        {
            GameObject row = Instantiate(rowPrefab, content);
            PlayerBox box = row.GetComponent<PlayerBox>();
            rowPool.Add(box);
        }
    }
}
