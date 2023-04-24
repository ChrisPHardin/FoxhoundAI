using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAI
{
    public class Animate
    {
        //animType
        //0 = idle
        //1 = talking
        //2 = angry
        //3 = surprised
        public static string DoAnim(int animType, string animChar)
        {
            Random animRnd = new Random();
            if (animType == 0)
            {
                int rnd = animRnd.Next(7);
                if (rnd == 0)
                {
                    return ("codex\\" + animChar + "\\mouth_closed3");
                }
                else if (rnd == 1)
                {
                    return ("codex\\" + animChar + "\\mouth_closed1");
                }
                else if (rnd >= 2)
                {
                    return ("codex\\" + animChar + "\\mouth_closed1");
                }
                else
                {
                    return ("codex\\" + animChar + "\\mouth_closed1");
                }
            }
            else if (animType == 1)
            {
                int rnd = animRnd.Next(2);
                if (rnd == 0)
                {
                    return ("codex\\" + animChar + "\\mouth_talk1");
                }
                else if (rnd == 1)
                {
                    return ("codex\\" + animChar + "\\mouth_talk2");
                }
            }
            else
            {
                return ("codex\\" + animChar + "\\mouth_closed1");
            }
            return ("codex\\" + animChar + "\\mouth_closed1");

        }
    
    }
}
