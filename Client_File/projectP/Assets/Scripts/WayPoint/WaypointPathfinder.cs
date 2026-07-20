using System.Collections.Generic;
using UnityEngine;

public static class WaypointPathfinder
{
    public static List<Waypoint> FindPath(Waypoint start, Waypoint goal)
    {
        if (start == null || goal == null)
            return null;

        if (start == goal)
            return new List<Waypoint> { start };

        var openSet = new List<Waypoint> { start };
        var closedSet = new HashSet<Waypoint>();
        var cameFrom = new Dictionary<Waypoint, Waypoint>();

        var gScore = new Dictionary<Waypoint, float> { [start] = 0f };
        var fScore = new Dictionary<Waypoint, float> { [start] = Heuristic(start, goal) };

        while (openSet.Count > 0)
        {
            Waypoint current = GetLowestFScore(openSet, fScore);

            if (current == goal)
                return ReconstructPath(cameFrom, current);

            openSet.Remove(current);
            closedSet.Add(current);

            if (current.Neighbors == null)
                continue;

            foreach (var neighbor in current.Neighbors)
            {
                if (neighbor == null || closedSet.Contains(neighbor))
                    continue;

                float tentativeG = gScore[current] + Vector3.Distance(current.transform.position, neighbor.transform.position);

                if (gScore.TryGetValue(neighbor, out float neighborG) && tentativeG >= neighborG)
                    continue;

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeG;
                fScore[neighbor] = tentativeG + Heuristic(neighbor, goal);

                if (!openSet.Contains(neighbor))
                    openSet.Add(neighbor);
            }
        }

        return null;
    }

    private static Waypoint GetLowestFScore(List<Waypoint> openSet, Dictionary<Waypoint, float> fScore)
    {
        Waypoint best = openSet[0];
        float bestScore = fScore.TryGetValue(best, out float score) ? score : float.MaxValue;

        for (int i = 1; i < openSet.Count; i++)
        {
            var candidate = openSet[i];
            float candidateScore = fScore.TryGetValue(candidate, out float s) ? s : float.MaxValue;

            if (candidateScore < bestScore)
            {
                best = candidate;
                bestScore = candidateScore;
            }
        }

        return best;
    }

    private static float Heuristic(Waypoint a, Waypoint b)
    {
        return Vector3.Distance(a.transform.position, b.transform.position);
    }

    private static List<Waypoint> ReconstructPath(Dictionary<Waypoint, Waypoint> cameFrom, Waypoint current)
    {
        var path = new List<Waypoint> { current };

        while (cameFrom.TryGetValue(current, out var previous))
        {
            current = previous;
            path.Add(current);
        }

        path.Reverse();
        return path;
    }
}
