using PLC.Commissioning.Lib.Abstractions;
using PLC.Commissioning.Lib.Siemens;
using PLC.Commissioning.Lib.Enums;
using System;

namespace PLC.Commissioning.Lib
{
    /// <summary>
    /// Factory class responsible for creating PLC controllers based on the specified manufacturer.
    /// </summary>
    public class PLCFactory
    {
        /// <summary>
        /// Creates a controller instance of type <typeparamref name="T"/> for the specified <see cref="Manufacturer"/>.
        /// </summary>
        /// <typeparam name="T">The type of PLC controller that implements <see cref="IPLCController"/>.</typeparam>
        /// <param name="manufacturer">The manufacturer of the PLC to create.</param>
        /// <returns>An instance of type <typeparamref name="T"/> corresponding to the specified manufacturer.</returns>
        /// <exception cref="ArgumentException">Thrown when an unsupported manufacturer is provided.</exception>
        /// <exception cref="InvalidCastException">Thrown when the created controller cannot be cast to the type <typeparamref name="T"/>.</exception>
        public static T CreateController<T>(Manufacturer manufacturer) where T : class, IPLCController
        {
            IPLCController controller;

            switch (manufacturer)
            {
                case Manufacturer.Siemens:
                    controller = new SiemensPLCController();
                    break;
                // Add cases for other manufacturers here:
                // case Manufacturer.Beckhoff:
                //     controller = new BeckhoffPLCController();
                //     break;
                // case Manufacturer.Rockwell:
                //     controller = new RockwellPLCController();
                //     break;
                default:
                    throw new ArgumentException("Unsupported manufacturer", nameof(manufacturer));
            }

            if (!(controller is T typedController))
            {
                throw new InvalidCastException($"The controller for {manufacturer} cannot be cast to {typeof(T).Name}");
            }

            return typedController;
        }
    }
}
