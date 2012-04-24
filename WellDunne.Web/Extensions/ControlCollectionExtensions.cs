using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Web.UI
{
    public static class ControlCollectionExtensions
    {
        public static IEnumerable<T> FindRecursivelyByType<T>(this Control control) where T : Control
        {
            Stack<Control> cStack = new Stack<Control>();
            cStack.Push(control);
            
            while (cStack.Count > 0)
            {
                Control c = cStack.Pop();
                // Provide this element in the enumerable:
                if (c.GetType() == typeof(T))
                    yield return (T)c;

                // Add this element's children to the stack to be traversed:
                foreach (Control child in c.Controls)
                {
                    cStack.Push(child);
                }
            }
        }
    }
}
