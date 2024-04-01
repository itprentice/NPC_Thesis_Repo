using System.Collections;
using UnityEngine;

/**
 * class that represent textual descriptions of the attached game object
 * These descriptions are represented as a Tree which is heavily derived from the Transform tree hierarchy 
 **/
public class DescriptionNode : MonoBehaviour
{
    [field: SerializeField]
    public string objectDescription {  get; private set; }

    /**
     * output: true if node has no parent or no parent that has a description, false otherwise
     **/
    public bool isRoot()
    {
        return transform.parent == null || !transform.parent.gameObject.TryGetComponent<DescriptionNode>(out _);
    }
    /**
     * finds and returns the uppermost root DescriptionNode of the tree this node is in
     * if parent is null or does not have a description, the node will return itself
     **/
    public DescriptionNode findRoot()
    {
        //more expensive (due to potentially calling TryGetComponent twice, but more readable and maintainable
        if (isRoot())
        {
            return this;
        }
        else
        {
            transform.parent.gameObject.TryGetComponent<DescriptionNode>(out DescriptionNode rootCandidate);
            return rootCandidate.findRoot();
        }
    }
    /**
     * returns the full text representation of the tree rooted at this node
     **/
    public string inorderRepresentation()
    {
        int midpoint = transform.childCount / 2;
        string repr = "";
        for (int i = 0; i < midpoint; i++)
        {
            DescriptionNode c = transform.GetChild(i).gameObject.GetComponent<DescriptionNode>();
            if (c != null )
            {
                repr += c.inorderRepresentation() + " ";
            }
        }
        repr += objectDescription + " ";
        for (int i = midpoint; i < transform.childCount; i++)
        {
            DescriptionNode c = transform.GetChild(i).gameObject.GetComponent<DescriptionNode>();
            if (c != null)
            {
                repr += c.inorderRepresentation() + " ";
            }
        }

        repr = repr.Remove(repr.Length - 1);

        return repr;
    }
}