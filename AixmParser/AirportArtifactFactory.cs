using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParseAIXM
{
    using AirportInformation.ViewModels;


    public class NullObject : AirportBase
    {
        
    }

    public static class AirportArtifactFactory
    {
        private static NullObject nullObject = new NullObject();

        public static AirportBase Create(string type)
        {
            switch (type)
            {
                case "aixm:AirportHeliport":
                    return new Airport();
                case "aixm:OrganisationAuthority":
                    return nullObject;
                case "aixm:Unit":
                    return nullObject;
                case "aixm:AirTrafficControlService":
                    return nullObject;
                case "aixm:RadioCommunicationChannel":
                    return new RadioCommunicationChannel();
                case "aixm:AirportSuppliesService":
                    return new AirportSuppliesService();
                case "aixm:Runway":
                    return new Runway();
                case "aixm:RunwayMarking":
                    return nullObject;
                case "aixm:TouchDownLiftOff":
                    return new TouchDownLiftOff();
                case "aixm:RunwayDirection":
                    return new RunwayDirection();
                case "aixm:Glidepath":
                    return nullObject;
                case "aixm:ApproachLightingSystem":
                    return nullObject;
                default:
                    return nullObject;
            }
        }
    }
}
