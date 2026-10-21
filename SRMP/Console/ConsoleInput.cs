using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SRMultiplayer
{
    public class ConsoleInput
    {
        public string inputString = "";
        public event Action<string> OnInputText;

        /// <summary>
        /// Handle updates the console
        /// This is to catch key presses and process them
        /// </summary>
        public void Update()
        {
            if (Console.KeyAvailable)
            {
                ConsoleKeyInfo consoleKeyInfo = Console.ReadKey();

                switch (consoleKeyInfo.Key)
                {
                    case ConsoleKey.Enter: this.OnEnter(); break;
                    case ConsoleKey.Backspace: this.OnBackspace(); break;
                    case ConsoleKey.Escape: this.OnEscape(); break;
                    case ConsoleKey.UpArrow: this.GetCommand(1); break;
                    case ConsoleKey.DownArrow: this.GetCommand(-1); break;
                    default:
                        //check if pressed key is a character
                        bool flag = consoleKeyInfo.KeyChar > '\0';
                        if (flag)
                        {
                            //if so at it tothe input line
                            this.inputString += consoleKeyInfo.KeyChar.ToString();
                            this.RedrawInputLine();
                        }
                        break;
                }
            }



        }


        #region Console Processors
        /// <summary>
        /// Clears out the current console line
        /// </summary>
        public void ClearLine()
        {
            Console.CursorLeft = 0;
            Console.Write(new string(' ', Console.BufferWidth));
            Console.CursorTop--;
            Console.CursorLeft = 0;
        }
        /// <summary>
        /// Used to redraw the input line incase we had to hold for console write lines
        /// </summary>
        public void RedrawInputLine()
        {
            bool flag = Console.CursorLeft > 0;
            if (flag)
            {
                this.ClearLine();
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("> ");
            Console.Write(this.inputString);
        }
        #endregion region

        #region Key Press Events
        /// <summary>
        /// Process Backspace pressed
        /// </summary>
        internal void OnBackspace()
        {
            bool flag = this.inputString.Length <= 0;
            if (!flag)
            {
                this.inputString = this.inputString.Substring(0, this.inputString.Length - 1);
                this.RedrawInputLine();
            }
        }
        /// <summary>
        /// Process Escape  pressed
        /// </summary>
        internal void OnEscape()
        {
            this.ClearLine();
            this.inputString = "";
            this.RedrawInputLine();
        }
        
        /// <summary>
        /// Process Enter pressed
        /// </summary>
        internal void OnEnter()
        {
            this.ClearLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("> " + this.inputString);
            string obj = this.inputString;
            this.inputString = "";
            bool flag = this.OnInputText != null;
            if (flag)
            {
                //on text inputed reset the search loc and cycle the search tree
                searchLoc = -1; //searchLoc set to -1 to always go to place 0 on first arrow
                AddToCommandTree(obj);

                this.OnInputText(obj);
            }
        }

        #endregion

        #region Command History Retrieval
        List<string> cmdTree = new List<string>();
        int maxCommands = 10;
        /// <summary>
        /// Manages the Command tree for the up and down arrows
        /// </summary>
        /// <param name="cmdText"></param>
        internal void AddToCommandTree(string cmdText)
        {
            cmdTree.Insert(0, cmdText);

            //if tree gets larger than 10 remove the 11th item
            if (cmdTree.Count > maxCommands) { cmdTree.RemoveAt(10); };
        }


        //handle internal cycling of last 10 commands
        int searchLoc = -1;
        /// <summary>
        /// Gets the command at the designaed slot from the current possition
        /// </summary>
        /// <param name="diff">Possition from the current Command </param>
        internal void GetCommand(int diff)
        {
            if (cmdTree.Count > 0)
            {
                searchLoc = searchLoc + diff;
                //prevent below 0 or over max position
                if (searchLoc > (cmdTree.Count - 1)) searchLoc = (cmdTree.Count - 1);
                if (searchLoc < 0) searchLoc = 0;

                //if a new location is found enter the search text in the input and redraw it.
                this.inputString = cmdTree[searchLoc];
                this.RedrawInputLine();
            }
            else
            {
                Console.WriteLine("cmdTree is empty");
            }
        }
        #endregion
    }
}