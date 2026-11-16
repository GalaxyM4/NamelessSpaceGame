using System.Collections.Generic;
using UnityEngine;

public class Mission
{
    public string name;
    public string information;
    public string urlInformation;
    public Texture2D imageReference;
    public float budget;
    public List<Artifact> requiredArtifacts;
}

public static class MissionsImplement
{
    public static IReadOnlyList<Mission> GetMissions = new Mission[]
    {
        new () {
            name = "The Collection",
            information = "Since landing on the Red Planet in 2021 to search for signs of ancient life, NASA's Perseverance Mars rover has driven farther and faster than any vehicle on the surface of another world. On Oct. 11, its odometer reached 28.10 miles (45.22 kilometers), beating the 28.06-mile (45.16-kilometer) record of NASA's Opportunity rover (2004-2018). And Perseverance covered that ground in about 5½ years, compared with almost 15 for its predecessor. This mission highlights mineral collection, honoring this milestone from 2021.",
            imageReference = Resources.Load<Texture2D>("marslike01ft"),
            urlInformation = "https://science.nasa.gov/mission/mars-2020-perseverance/",
            budget = 300,
            requiredArtifacts = new() {
                new MarsRover()
            }
        },
        
        new () {
            name = "The Collection 2",
            information = "Since landing on the Red Planet in 2021 to search for signs of ancient life, NASA's Perseverance Mars rover has driven farther and faster than any vehicle on the surface of another world. On Oct. 11, its odometer reached 28.10 miles (45.22 kilometers), beating the 28.06-mile (45.16-kilometer) record of NASA's Opportunity rover (2004-2018). And Perseverance covered that ground in about 5½ years, compared with almost 15 for its predecessor. This mission highlights mineral collection, honoring this milestone from 2021.",
            imageReference = Resources.Load<Texture2D>("marslike01ft"),
            urlInformation = "https://science.nasa.gov/mission/mars-2020-perseverance/",
            budget = 300,
            requiredArtifacts = new() {
                new MarsRover()
            }
        }
    };
}