using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace SRMultiplayer.Networking
{
    public class NetworkClientUI : MonoBehaviour
    {
        public List<GameObject> Panels = new List<GameObject>();

        public GameObject UsernamePanel;
        public InputField UsernameInput;

        public GameObject ErrorPanel;
        public Text ErrorText;

        private void Awake()
        {
            UsernamePanel = transform.Find("Background/UsernamePanel").gameObject;
            UsernameInput = UsernamePanel.transform.Find("UsernameInput").GetComponent<InputField>();
            UsernamePanel.transform.Find("ConfirmButton").GetComponent<Button>().onClick.AddListener(OnUsernameConfirm);

            ErrorPanel = transform.Find("Background/ErrorPanel").gameObject;
            ErrorText = ErrorPanel.transform.Find("ErrorText").GetComponent<Text>();
            ErrorPanel.transform.Find("CloseButton").GetComponent<Button>().onClick.AddListener(HideError);

            Panels.Add(UsernamePanel);

            UsernameInput.text = PlayerPrefs.GetString("MultiplayerUsername", "");
            if(string.IsNullOrWhiteSpace(UsernameInput.text))
            {
                ChangeUsername();
            }
            else
            {
                //Connect UI
            }
        }

        private void OnUsernameConfirm()
        {
            if (!string.IsNullOrWhiteSpace(UsernameInput.text) && UsernameInput.text.Length > 1 && UsernameInput.text.Length < 30)
            {
                PlayerPrefs.SetString("MultiplayerUsername", UsernameInput.text);
                SRMP.Log("UsernameInput: " + UsernameInput.text);
                HidePanels();
                //Connect UI
            }
            else
            {
                ShowError("Username has to be between 1 and 30 letters long");
            }
        }

        private void ChangeUsername()
        {
            HidePanels();
            UsernamePanel.SetActive(true);
        }

        private void ShowError(string error)
        {
            ErrorText.text = error;
            ErrorPanel.SetActive(true);
        }

        private void HideError()
        {
            ErrorPanel.SetActive(false);
        }

        private void HidePanels()
        {
            foreach(var panel in Panels)
            {
                panel.SetActive(false);
            }
        }
    }
}
