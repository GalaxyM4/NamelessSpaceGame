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
            name = "La Recolección",
            information = "Desde que aterrizó en el Planeta Rojo en 2021 para buscar señales de vida antigua, el rover Perseverance Mars de la NASA ha recorrido más lejos y más rápido que cualquier vehículo en la superficie de otro mundo. En octubre. 11, su odómetro alcanzó las 28,10 millas (45,22 kilómetros), superando el récord de 28,06 millas (45,16 kilómetros) del rover Opportunity de la NASA (2004-2018). Y Perseverance ha cubierto el terreno en aproximadamente 5½ años, en comparación con casi 15 de su predecesor. Esta misión destaca la recolección de minerales homenajeando este hito en 2021.",
            imageReference = Resources.Load<Texture2D>("marslike01ft"),
            urlInformation = "https://science.nasa.gov/mission/mars-2020-perseverance/",
            budget = 300,
            requiredArtifacts = new() {
                new MarsRover()
            }
        },
        
        new () {
            name = "La Recolección 2",
            information = "Desde que aterrizó en el Planeta Rojo en 2021 para buscar señales de vida antigua, el rover Perseverance Mars de la NASA ha recorrido más lejos y más rápido que cualquier vehículo en la superficie de otro mundo. En octubre. 11, su odómetro alcanzó las 28,10 millas (45,22 kilómetros), superando el récord de 28,06 millas (45,16 kilómetros) del rover Opportunity de la NASA (2004-2018). Y Perseverance ha cubierto el terreno en aproximadamente 5½ años, en comparación con casi 15 de su predecesor. Esta misión destaca la recolección de minerales homenajeando este hito en 2021.",
            imageReference = Resources.Load<Texture2D>("marslike01ft"),
            urlInformation = "https://science.nasa.gov/mission/mars-2020-perseverance/",
            budget = 300,
            requiredArtifacts = new() {
                new MarsRover()
            }
        }
    };
}