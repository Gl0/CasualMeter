using Tera.Game.Messages;

namespace CasualMeter.Tracker
{
    public static class MessageExtensions
    {
        public static bool IsValid(this EachSkillResultServerMessage message, DamageTracker tracker = null)
        {
            return message != null && !message.IsUseless && //stuff like warrior DFA
                   (tracker?.FirstAttack != null || (!message.IsHeal && message.Amount > 0)) &&//only record first hit is it's a damage hit (heals occurring outside of fights)
                   !(message.Target.Equals(message.Source) && !message.IsHeal && message.Amount > 0);//disregard damage dealt to self (gunner self destruct)
        }
    }
}
