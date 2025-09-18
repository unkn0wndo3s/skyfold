using UnityEngine;
using System.Collections.Generic;

public enum ColliderType
{
    MeshCollider,
    BoxCollider,
    CapsuleCollider,
    SphereCollider,
    CustomCollider
}

[System.Serializable]
public class ColliderSettings
{
    [Header("Mesh Detection")]
    public bool autoDetectMesh = true;
    public Mesh targetMesh;
    public Transform meshParent;
    
    [Header("Collider Settings")]
    public bool isTrigger = false;
    public PhysicsMaterial physicsMaterial;
    public bool convex = false; // Convex pour les collisions avec d'autres objets
    public ColliderType colliderType = ColliderType.MeshCollider; // Type de collider à générer
    
    [Header("Custom Collider Settings")]
    public bool createCustomCollider = true; // Créer un collider personnalisé optimisé
    public float precisionLevel = 0.1f; // Niveau de précision (0.01 = très précis, 1.0 = moins précis)
    public bool useMultipleColliders = false; // Utiliser plusieurs colliders pour des formes complexes
    public int maxColliderCount = 3; // Nombre maximum de colliders à créer
    
    [Header("Optimization")]
    public bool optimizeForPerformance = true;
    public int maxVertices = 1000; // Limite pour l'optimisation
    public bool removeDuplicateVertices = true;
    public float vertexMergeThreshold = 0.001f;
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    public Color debugColor = Color.green;
}

public class PreciseColliderGenerator : MonoBehaviour
{
    [Header("Collider Generation")]
    public ColliderSettings settings = new ColliderSettings();
    
    [Header("Generated Collider Info")]
    [SerializeField] private List<Collider> generatedColliders = new List<Collider>();
    [SerializeField] private int originalVertexCount;
    [SerializeField] private int optimizedVertexCount;
    
    private Mesh originalMesh;
    private Mesh optimizedMesh;
    
    void Start()
    {
        GeneratePreciseCollider();
    }
    
    [ContextMenu("Generate Precise Collider")]
    public void GeneratePreciseCollider()
    {
        // Supprimer l'ancien collider s'il existe
        RemoveExistingCollider();
        
        // Trouver le mesh cible
        Mesh targetMesh = FindTargetMesh();
        if (targetMesh == null)
        {
            Debug.LogError("Aucun mesh trouvé pour générer le collider précis!");
            return;
        }
        
        // Sauvegarder le mesh original
        originalMesh = targetMesh;
        originalVertexCount = targetMesh.vertexCount;
        
        // Optimiser le mesh si nécessaire
        optimizedMesh = OptimizeMesh(targetMesh);
        optimizedVertexCount = optimizedMesh.vertexCount;
        
        // Créer le collider selon le type choisi
        CreateCustomCollider(optimizedMesh);
        
        // Afficher les informations de debug
        if (settings.showDebugInfo)
        {
            ShowDebugInfo();
        }
    }
    
    private Mesh FindTargetMesh()
    {
        if (!settings.autoDetectMesh && settings.targetMesh != null)
        {
            return settings.targetMesh;
        }
        
        // Chercher spécifiquement dans le premier enfant direct
        if (transform.childCount > 0)
        {
            Transform firstChild = transform.GetChild(0);
            MeshFilter childFilter = firstChild.GetComponent<MeshFilter>();
            MeshRenderer childRenderer = firstChild.GetComponent<MeshRenderer>();
            
            // Priorité au MeshRenderer du premier enfant
            if (childRenderer != null && childFilter != null && childFilter.sharedMesh != null)
            {
                Debug.Log($"Mesh trouvé dans l'enfant direct: {firstChild.name}");
                return childFilter.sharedMesh;
            }
            
            // Fallback sur MeshFilter du premier enfant
            if (childFilter != null && childFilter.sharedMesh != null)
            {
                Debug.Log($"Mesh trouvé dans l'enfant direct (MeshFilter): {firstChild.name}");
                return childFilter.sharedMesh;
            }
        }
        
        // Si aucun enfant direct avec mesh, chercher dans tous les enfants
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
        
        // Priorité aux MeshRenderer (modèles visibles)
        if (meshRenderers.Length > 0)
        {
            foreach (MeshRenderer renderer in meshRenderers)
            {
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh != null)
                {
                    Debug.Log($"Mesh trouvé dans les enfants: {renderer.name}");
                    return filter.sharedMesh;
                }
            }
        }
        
        // Fallback sur tous les MeshFilter
        if (meshFilters.Length > 0)
        {
            foreach (MeshFilter filter in meshFilters)
            {
                if (filter.sharedMesh != null)
                {
                    Debug.Log($"Mesh trouvé dans les enfants (MeshFilter): {filter.name}");
                    return filter.sharedMesh;
                }
            }
        }
        
        // Chercher dans le parent spécifié
        if (settings.meshParent != null)
        {
            MeshFilter parentFilter = settings.meshParent.GetComponent<MeshFilter>();
            if (parentFilter != null && parentFilter.sharedMesh != null)
            {
                Debug.Log($"Mesh trouvé dans le parent spécifié: {settings.meshParent.name}");
                return parentFilter.sharedMesh;
            }
        }
        
        Debug.LogWarning("Aucun mesh trouvé dans l'enfant direct ou les enfants!");
        return null;
    }
    
    private Mesh OptimizeMesh(Mesh originalMesh)
    {
        if (!settings.optimizeForPerformance)
        {
            return originalMesh;
        }
        
        // Créer une copie du mesh pour l'optimisation
        Mesh optimized = new Mesh();
        optimized.name = originalMesh.name + "_Optimized";
        
        // Copier les données de base
        optimized.vertices = originalMesh.vertices;
        optimized.triangles = originalMesh.triangles;
        optimized.normals = originalMesh.normals;
        optimized.uv = originalMesh.uv;
        optimized.uv2 = originalMesh.uv2;
        optimized.colors = originalMesh.colors;
        optimized.tangents = originalMesh.tangents;
        
        // Optimisations
        if (settings.removeDuplicateVertices)
        {
            RemoveDuplicateVertices(optimized);
        }
        
        // Limiter le nombre de vertices si nécessaire
        if (optimized.vertexCount > settings.maxVertices)
        {
            SimplifyMesh(optimized);
        }
        
        // Recalculer les normales et bounds
        optimized.RecalculateNormals();
        optimized.RecalculateBounds();
        
        return optimized;
    }
    
    private void RemoveDuplicateVertices(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Vector2[] uv = mesh.uv;
        Color[] colors = mesh.colors;
        Vector4[] tangents = mesh.tangents;
        int[] triangles = mesh.triangles;
        
        List<Vector3> newVertices = new List<Vector3>();
        List<Vector3> newNormals = new List<Vector3>();
        List<Vector2> newUV = new List<Vector2>();
        List<Color> newColors = new List<Color>();
        List<Vector4> newTangents = new List<Vector4>();
        List<int> newTriangles = new List<int>();
        
        Dictionary<int, int> vertexMap = new Dictionary<int, int>();
        
        for (int i = 0; i < vertices.Length; i++)
        {
            bool isDuplicate = false;
            int duplicateIndex = -1;
            
            // Chercher un vertex similaire
            for (int j = 0; j < newVertices.Count; j++)
            {
                if (Vector3.Distance(vertices[i], newVertices[j]) < settings.vertexMergeThreshold)
                {
                    isDuplicate = true;
                    duplicateIndex = j;
                    break;
                }
            }
            
            if (isDuplicate)
            {
                vertexMap[i] = duplicateIndex;
            }
            else
            {
                vertexMap[i] = newVertices.Count;
                newVertices.Add(vertices[i]);
                if (normals.Length > 0) newNormals.Add(normals[i]);
                if (uv.Length > 0) newUV.Add(uv[i]);
                if (colors.Length > 0) newColors.Add(colors[i]);
                if (tangents.Length > 0) newTangents.Add(tangents[i]);
            }
        }
        
        // Reconstruire les triangles
        for (int i = 0; i < triangles.Length; i++)
        {
            newTriangles.Add(vertexMap[triangles[i]]);
        }
        
        // Appliquer les nouvelles données
        mesh.vertices = newVertices.ToArray();
        mesh.triangles = newTriangles.ToArray();
        if (newNormals.Count > 0) mesh.normals = newNormals.ToArray();
        if (newUV.Count > 0) mesh.uv = newUV.ToArray();
        if (newColors.Count > 0) mesh.colors = newColors.ToArray();
        if (newTangents.Count > 0) mesh.tangents = newTangents.ToArray();
    }
    
    private void SimplifyMesh(Mesh mesh)
    {
        // Simplification basique - garder seulement les vertices les plus importants
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        
        if (vertices.Length <= settings.maxVertices)
            return;
        
        // Calculer l'importance de chaque vertex basée sur le nombre de triangles qui l'utilisent
        Dictionary<int, int> vertexUsage = new Dictionary<int, int>();
        
        for (int i = 0; i < triangles.Length; i++)
        {
            int vertexIndex = triangles[i];
            if (vertexUsage.ContainsKey(vertexIndex))
                vertexUsage[vertexIndex]++;
            else
                vertexUsage[vertexIndex] = 1;
        }
        
        // Trier les vertices par importance
        List<KeyValuePair<int, int>> sortedVertices = new List<KeyValuePair<int, int>>(vertexUsage);
        sortedVertices.Sort((a, b) => b.Value.CompareTo(a.Value));
        
        // Garder seulement les vertices les plus importants
        HashSet<int> keepVertices = new HashSet<int>();
        for (int i = 0; i < Mathf.Min(settings.maxVertices, sortedVertices.Count); i++)
        {
            keepVertices.Add(sortedVertices[i].Key);
        }
        
        // Créer un nouveau mesh avec seulement les vertices importants
        List<Vector3> newVertices = new List<Vector3>();
        Dictionary<int, int> vertexMapping = new Dictionary<int, int>();
        
        for (int i = 0; i < vertices.Length; i++)
        {
            if (keepVertices.Contains(i))
            {
                vertexMapping[i] = newVertices.Count;
                newVertices.Add(vertices[i]);
            }
        }
        
        // Reconstruire les triangles (en gardant seulement ceux qui utilisent les vertices gardés)
        List<int> newTriangles = new List<int>();
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int v1 = triangles[i];
            int v2 = triangles[i + 1];
            int v3 = triangles[i + 2];
            
            if (keepVertices.Contains(v1) && keepVertices.Contains(v2) && keepVertices.Contains(v3))
            {
                newTriangles.Add(vertexMapping[v1]);
                newTriangles.Add(vertexMapping[v2]);
                newTriangles.Add(vertexMapping[v3]);
            }
        }
        
        mesh.vertices = newVertices.ToArray();
        mesh.triangles = newTriangles.ToArray();
    }
    
    private void CreateCustomCollider(Mesh mesh)
    {
        // Vider la liste des colliders existants
        generatedColliders.Clear();
        
        if (settings.createCustomCollider)
        {
            // Créer un collider personnalisé basé sur l'analyse du mesh
            CreatePreciseCustomCollider(mesh);
        }
        else
        {
            // Créer le type de collider spécifié
            CreateStandardCollider(mesh);
        }
        
        Debug.Log($"Collider personnalisé généré avec {generatedColliders.Count} collider(s)");
    }
    
    private void CreatePreciseCustomCollider(Mesh mesh)
    {
        // Analyser la géométrie du mesh pour déterminer le meilleur type de collider
        Bounds meshBounds = mesh.bounds;
        Vector3 size = meshBounds.size;
        Vector3 center = meshBounds.center;
        
        // Calculer les ratios pour déterminer la forme
        float maxDimension = Mathf.Max(size.x, size.y, size.z);
        float minDimension = Mathf.Min(size.x, size.y, size.z);
        float aspectRatio = maxDimension / minDimension;
        
        Debug.Log($"Analyse du mesh - Taille: {size}, Ratio: {aspectRatio:F2}");
        
        // Décider du type de collider basé sur la forme
        if (settings.useMultipleColliders && mesh.vertexCount > 500)
        {
            CreateMultipleColliders(mesh);
        }
        else if (aspectRatio > 3f && size.y > size.x && size.y > size.z)
        {
            // Forme allongée verticalement - Capsule
            CreateCapsuleCollider(meshBounds);
        }
        else if (Mathf.Abs(size.x - size.y) < 0.1f && Mathf.Abs(size.y - size.z) < 0.1f)
        {
            // Forme sphérique - Sphere
            CreateSphereCollider(meshBounds);
        }
        else
        {
            // Forme complexe - MeshCollider ou BoxCollider selon la précision
            if (settings.precisionLevel < 0.5f)
            {
                CreateMeshCollider(mesh);
            }
            else
            {
                CreateBoxCollider(meshBounds);
            }
        }
    }
    
    private void CreateMultipleColliders(Mesh mesh)
    {
        // Diviser le mesh en plusieurs parties pour créer des colliders plus simples
        Vector3[] vertices = mesh.vertices;
        Bounds meshBounds = mesh.bounds;
        
        // Diviser en 3 parties selon l'axe le plus long
        Vector3 size = meshBounds.size;
        int splitAxis = 0; // 0=X, 1=Y, 2=Z
        if (size.y > size.x && size.y > size.z) splitAxis = 1;
        else if (size.z > size.x && size.z > size.y) splitAxis = 2;
        
        int colliderCount = Mathf.Min(settings.maxColliderCount, 3);
        float step = size[splitAxis] / colliderCount;
        
        for (int i = 0; i < colliderCount; i++)
        {
            float min = meshBounds.min[splitAxis] + i * step;
            float max = meshBounds.min[splitAxis] + (i + 1) * step;
            
            // Créer un collider pour cette section
            Vector3 sectionCenter = meshBounds.center;
            sectionCenter[splitAxis] = (min + max) / 2f;
            
            Vector3 sectionSize = size;
            sectionSize[splitAxis] = step;
            
            Bounds sectionBounds = new Bounds(sectionCenter, sectionSize);
            CreateBoxCollider(sectionBounds);
        }
    }
    
    private void CreateMeshCollider(Mesh mesh)
    {
        MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
        meshCollider.isTrigger = settings.isTrigger;
        meshCollider.convex = settings.convex;
        
        if (settings.physicsMaterial != null)
        {
            meshCollider.material = settings.physicsMaterial;
        }
        
        generatedColliders.Add(meshCollider);
        Debug.Log("MeshCollider créé");
    }
    
    private void CreateBoxCollider(Bounds bounds)
    {
        BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
        boxCollider.center = bounds.center;
        boxCollider.size = bounds.size;
        boxCollider.isTrigger = settings.isTrigger;
        
        if (settings.physicsMaterial != null)
        {
            boxCollider.material = settings.physicsMaterial;
        }
        
        generatedColliders.Add(boxCollider);
        Debug.Log($"BoxCollider créé - Centre: {bounds.center}, Taille: {bounds.size}");
    }
    
    private void CreateCapsuleCollider(Bounds bounds)
    {
        CapsuleCollider capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
        capsuleCollider.center = bounds.center;
        capsuleCollider.height = bounds.size.y;
        capsuleCollider.radius = Mathf.Min(bounds.size.x, bounds.size.z) / 2f;
        capsuleCollider.isTrigger = settings.isTrigger;
        
        if (settings.physicsMaterial != null)
        {
            capsuleCollider.material = settings.physicsMaterial;
        }
        
        generatedColliders.Add(capsuleCollider);
        Debug.Log($"CapsuleCollider créé - Centre: {bounds.center}, Hauteur: {capsuleCollider.height}, Rayon: {capsuleCollider.radius}");
    }
    
    private void CreateSphereCollider(Bounds bounds)
    {
        SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
        sphereCollider.center = bounds.center;
        sphereCollider.radius = Mathf.Min(bounds.size.x, bounds.size.y, bounds.size.z) / 2f;
        sphereCollider.isTrigger = settings.isTrigger;
        
        if (settings.physicsMaterial != null)
        {
            sphereCollider.material = settings.physicsMaterial;
        }
        
        generatedColliders.Add(sphereCollider);
        Debug.Log($"SphereCollider créé - Centre: {bounds.center}, Rayon: {sphereCollider.radius}");
    }
    
    private void CreateStandardCollider(Mesh mesh)
    {
        switch (settings.colliderType)
        {
            case ColliderType.MeshCollider:
                CreateMeshCollider(mesh);
                break;
            case ColliderType.BoxCollider:
                CreateBoxCollider(mesh.bounds);
                break;
            case ColliderType.CapsuleCollider:
                CreateCapsuleCollider(mesh.bounds);
                break;
            case ColliderType.SphereCollider:
                CreateSphereCollider(mesh.bounds);
                break;
        }
    }
    
    private void RemoveExistingCollider()
    {
        // Supprimer tous les colliders générés
        foreach (Collider col in generatedColliders)
        {
            if (col != null)
            {
                DestroyImmediate(col);
            }
        }
        generatedColliders.Clear();
        
        // Supprimer tous les autres colliders existants
        Collider[] allColliders = GetComponents<Collider>();
        foreach (Collider col in allColliders)
        {
            if (col != null)
            {
                DestroyImmediate(col);
            }
        }
    }
    
    private void ShowDebugInfo()
    {
        if (generatedColliders.Count > 0)
        {
            Debug.Log($"=== COLLIDER PERSONNALISÉ GÉNÉRÉ ===");
            Debug.Log($"Mesh original: {originalVertexCount} vertices");
            Debug.Log($"Mesh optimisé: {optimizedVertexCount} vertices");
            Debug.Log($"Réduction: {((float)(originalVertexCount - optimizedVertexCount) / originalVertexCount * 100):F1}%");
            Debug.Log($"Triangles: {optimizedMesh.triangles.Length / 3}");
            Debug.Log($"Nombre de colliders: {generatedColliders.Count}");
            
            for (int i = 0; i < generatedColliders.Count; i++)
            {
                Collider col = generatedColliders[i];
                if (col != null)
                {
                    Debug.Log($"Collider {i + 1}: {col.GetType().Name} - Trigger: {col.isTrigger}");
                }
            }
        }
    }
    
    void OnDrawGizmos()
    {
        if (settings.showDebugInfo && generatedColliders.Count > 0)
        {
            Gizmos.color = settings.debugColor;
            
            // Dessiner tous les colliders générés
            foreach (Collider col in generatedColliders)
            {
                if (col != null)
                {
                    DrawColliderGizmo(col);
                }
            }
        }
    }
    
    private void DrawColliderGizmo(Collider collider)
    {
        Gizmos.matrix = Matrix4x4.identity;
        
        if (collider is BoxCollider boxCollider)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
        }
        else if (collider is SphereCollider sphereCollider)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireSphere(sphereCollider.center, sphereCollider.radius);
        }
        else if (collider is CapsuleCollider capsuleCollider)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Vector3 center = capsuleCollider.center;
            float height = capsuleCollider.height;
            float radius = capsuleCollider.radius;
            
            // Dessiner la capsule comme deux sphères + cylindre
            Vector3 top = center + Vector3.up * (height / 2f - radius);
            Vector3 bottom = center - Vector3.up * (height / 2f - radius);
            
            Gizmos.DrawWireSphere(top, radius);
            Gizmos.DrawWireSphere(bottom, radius);
            
            // Cylindre entre les deux sphères
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                Gizmos.DrawLine(top + offset, bottom + offset);
            }
        }
        else if (collider is MeshCollider meshCollider)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Mesh mesh = meshCollider.sharedMesh;
            if (mesh != null)
            {
                Vector3[] vertices = mesh.vertices;
                int[] triangles = mesh.triangles;
                
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    Vector3 v1 = vertices[triangles[i]];
                    Vector3 v2 = vertices[triangles[i + 1]];
                    Vector3 v3 = vertices[triangles[i + 2]];
                    
                    Gizmos.DrawLine(v1, v2);
                    Gizmos.DrawLine(v2, v3);
                    Gizmos.DrawLine(v3, v1);
                }
            }
        }
    }
    
    [ContextMenu("Remove Collider")]
    public void RemoveCollider()
    {
        RemoveExistingCollider();
        Debug.Log("Colliders personnalisés supprimés");
    }
    
    [ContextMenu("Regenerate Collider")]
    public void RegenerateCollider()
    {
        GeneratePreciseCollider();
    }
}
