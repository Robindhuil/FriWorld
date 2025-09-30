// using UnityEngine;

// public class TestManager : MonoBehaviour
// {
//     public static TestManager Instance { get; private set; }

//     private void Awake()
//     {
//         if (Instance != null && Instance != this)
//         {
//             Destroy(gameObject);
//             return;
//         }
//         Instance = this;
//         DontDestroyOnLoad(gameObject);
//     }

//     public void ActivateQuest()
//     {
//         Debug.Log("Quest aktivovaný!");
//         // Sem si dopíš vlastnú logiku
//     }
// }
