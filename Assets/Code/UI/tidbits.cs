using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class tidbits
{
    public static Dictionary<string, string> bodyInfo = new Dictionary<string, string>() {
        {"Mercury", "Only a touch smaller than our own moon, Mercury is the closest planet to the sun and the first planet in our solar system. Because of its very dense atmosphere, it can repel some of the Sun's radiation and thus is not the hottest planet in our solar system."},
        {"Venus", "With a thick atmosphere of greenhouse gasses, Venus retains much heat, making it the hottest planet in the Solar System. While both Venus and Earth appoximately share the same mass and desnity, Venus spins in the opposite direction of Earth and many other planets in our Solar System."},
        {"Earth", "The planet Earth has been a beacon for humanity for thousands of years, and this is the launching point for the Artemis Missions."},
        {"Mars", "It is theorized that many billions of years ago Mars had an atmosphere, a warmer climate and water on its surface. While Mars does not have any discovered biological life, it is full of robotic life."},
        {"Jupiter", "Twice the size of any other planet in the Solar System, Jupiter is a gas giant with a cold atmosphere of hydrogen and helium. Larger than the earth, the Great Red Spot swirls on its surface, a raging storm of ammonia and water clouds."},
        {"Saturn", "Saturn, famous for its dazzling rings of ice and rock, is a gas giant mostly consiting of hydrogen and helium. We humans have left our footprint on Saturn as two tons of mass was contributed to the planet by the Cassini Spacecraft."},
        {"Uranus", "Looking up at the night sky through a telescope William Herschel spotted Uranus, another gas giant. Uranus rotates on its side, a 90 degree tilt from what was perviously expected, and has rings."},
        {"Neptune", "First discovered by mathematical calulations, the farthest planet in our Solar System (sorry Pluto) is Neptune, a ice giant. Because of its massive distance from the sun, Neptune is a dark and cold planet battered by supersonice winds on its surface."},

        {"Deimos", "The Smaller of the two moons orbiting Mars, Deimos looks like asteroid orbiting around its parent planet. In greek mythology, Deimos is the brother of Phobos and son of Mars: Deimos means dread"},
        {"Phobos", "Phobos, meaning fear in greek, is the larger of Mars's two moons. Mars, watch out! Phobos is going to crash into you (in 50 million years) !"},

        {"Europa", "Is there Life beyond Earth? Nobody knows for sure, but Europa seems to be a good place to look. Europa, a Jovian moon, has a ice shell (15-25km thick) and underneath, liquid water. It's ocean is vast, two times as much water than is on Earth."},
        {"Io", "Caught in a dance between the gravity of Jupiter, Europa, and Ganymede, the Jovian moon Io is the most volcanically active place in the Solar System. If you visit, definitely bring an umbrella, lava fountains can spew the burning liquid dozens of miles into the air."},
        {"Ganymede", "The Jovian mooon Ganymede is massive, larger than Mercury and Pluto, infact it is so massive that it has its own magnetic feild. Like Europa, Ganymede also has an ocean under an icy surface."},
        {"Callisto", "Another Jovian moon with a seceret ocean, Callisto was first thought to be dead, but recently scientists believe that there is a chance it harbors life. Its surface has been battered by many meteor stikes and the surface impacts have left their marks on this moon with the oldest landscape in the Solar System."},

        {"Mimas", "Mimas is the smallest and innermost moon of saturn, consisting almost excusively of water ice. Star Wars Fans will be exited to see this Saturnian moon which looks much like the Death Star beause of a supermassive crater on its surface."},
        {"Enceladus", "Responsible for most of Saturns E ring, Enceladus is an icy Saturnian moon with an ocean underneath its surface. Enceladus ejects its ocean water into space at its south pole, and because of this, scientists have been able to sample this ejected water and have determined it contains many necessary ingredients for life."},
        {"Tethys", "Cold, airless and battered, the Saturnian moon Tethys is made mostly of water ice like Mimas. Thethys has two smaller moons orbiting it at its L4 and L5. (Does this make saturn a grandpa?)"},
        {"Hyperion", "'Rotation period = Chaotic' as the JPL Horizions system so aptly put it. Hyperion, a Saturnian moon looking much like an asteroid, is theorized to be a broken part of a former moon of Saturn, but for now all we know for sure is that it tumbles like there is no tommorow"},
        {"Iapetus", "Now you see it, now you don't, Iapetus's leading hemisphere has an abledo (or reflectivity) 10x less then its trailing hempishere, so the moon can only be seen when the trailing hemisphere is facing Earth. Because of its morbidly slow rotation, the ice on the side of Iapetus that faces the sun melts and goes to the other, creating the extreme difference in albedo."},
        {"Titan", "While the atmosphere is made mostly of nitrogen like earth, on the Saturnian moon Titan, it rains hydrocarbons like methane and ethane creating giant surface lakes of the liquids, which could harbor life with a different chemistry than we have ever seen. Underneath these lakes may be reseviors of water capable of harboring life." },

        {"Sun", "It's the sun, AH I BURNED MY EYES!"},

        {"Ariel", "Uranian Moon."},
        {"Umbriel", "Uranian Moon."},
        {"Titania", "Uranian Moon."},
        {"Oberon", "Uranian Moon."},
        {"Miranda", "Uranian Moon."},

        {"Triton", "Neptunian Moon."},
        {"Proteus", "Neptunian Moon."},

        {"Pluto", "A dwarf planet."},
        {"Charon", "Plutonian Moon."},

        {"HLS-Docked", "A satellite."},
        {"HLS-Surface", "A satellite."},
        {"HLS-NRHO", "A satellite."},
        {"HLS-Descent", "A satellite."},
        {"HLS-Ascent", "A satellite."},

        {"Orion-Docked", "A satellite."},
        {"Orion-NRHO", "A satellite."},
        {"Orion-Transit-O", "A satellite."},
        {"Orion-Transit-R", "A satellite."},

        {"Gateway", "A satellite."},
        {"MAVEM", "A satellite."},
        {"LRO", "A satellite."},
        {"Mars Express", "A satellite."},

        {"Luna", "The moon is a natural satelite of Earth, about 239,000 miles from Earth on avg. Famous for controlling tides, the moon was formed after a collision between Earth and a planet known as Theia.  Humans have explored the moon via the Apollo Era Missions, however this is soon to change with the planned Artemis Missions."},
        {"LCN-1", "LCN (Lunar Communications Network) is part of a constellation of satelites that brings communications capabilities to the moon."},
        {"LCN-2", "LCN (Lunar Communications Network) is part of a constellation of satelites that brings communications capabilities to the moon."},
        {"LCN-3", "LCN (Lunar Communications Network) is part of a constellation of satelites that brings communications capabilities to the moon."},
        {"CubeSat-1", "This cubesat is part of a class of nano-satelites known as a cubesat used to explore space in a smaller and less expensive formfactor. This particular satelite is testing a potential Artemis orbit."},
        {"CubeSat-2", "This cubesat is part of a class of nano-satelites known as a cubesat used to explore space in a smaller and less expensive formfactor. This particular satelite is testing a potential Artemis orbit."},


    };
}
