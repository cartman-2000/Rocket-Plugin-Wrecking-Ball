using SDG.Unturned;
using UnityEngine;

namespace ApokPT.RocketPlugins
{
    class Destructible
    {
        public Destructible(Transform transform, char type, InteractableVehicle vehicle = null, Zombie zombie = null)
        {
            Transform = transform;
            Type = type;
            Vehicle = vehicle;
            Zombie = zombie;
        }

        public Zombie Zombie { get; private set; }
        public InteractableVehicle Vehicle { get; private set; }
        public Transform Transform { get; private set; }
        public char Type { get; private set; }
    }
}
