﻿using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace WobblyManhunt.ChatLog
{
    public class ChatLogItem : MonoBehaviour
    {
        public Text text;

        public void Start()
        {
            
        }

        public IEnumerator destroy()
        {
            yield return new WaitForSeconds(4);
            Destroy(gameObject);
        }
    }
}
