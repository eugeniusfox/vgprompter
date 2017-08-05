using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VGPrompter {

    public interface IEntityType { }

    public interface ICharacter : IEntityType { }
    public interface ILocation : IEntityType { }
    public interface IItem : IEntityType { }


    public abstract class Entity<T> where T : IEntityType {

        public string Id { get; private set; }
        public string Name { get; private set; }

        public Entity(string id, string name = null) {
            Id = id;
            Name = name;
        }

    }

    public class Tag {

        public string Id { get; private set; }
        public List<Tag> Children { get; private set; }

        public Tag(string id, List<Tag> children = null) {
            Id = id;
            Children = children;
        }

    }

    public class EntityManager {

        public List<Entity<ICharacter>> Characters { get; private set; }

        public List<string> GetIds() {
            return Characters.Select(c => c.Id).ToList();
        }

    }

}
