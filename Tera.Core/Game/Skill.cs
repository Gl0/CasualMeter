// Copyright (c) Gothos
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Tera.Game
{
    public class Skill
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public bool? IsChained { get; private set; }
        public string Detail { get; private set; }
        internal Skill(int id, string name, bool? isChained = null, string detail = "")
        {
            Id = id;
            Name = name;
            IsChained = isChained;
            Detail = detail;
        }
    }

    public class UserSkill : Skill
    {
        public RaceGenderClass RaceGenderClass { get; private set; }

        public UserSkill(int id, RaceGenderClass raceGenderClass, string name, bool? isChained=null,string detail="")
            : base(id, name,isChained,detail)
        {
            RaceGenderClass = raceGenderClass;
        }

        public override bool Equals(object obj)
        {
            var other = obj as UserSkill;
            if (other == null)
                return false;
            return (Id == other.Id) && (RaceGenderClass.Equals(other.RaceGenderClass));
        }

        public override int GetHashCode()
        {
            return Id + RaceGenderClass.GetHashCode();
        }
    }
}