/// An array is a variable that holds multiple elements of the same type. Elements can be accesed by index and can be modified. <google C# arrays>More info<>.

int[] a1 = { 1, 3, 5, 7, 9 }; //array variable a1 has 5 elements of type int
var a2 = new int[10]; //array variable a2 has 10 elements of type int, all 0 (default value of that type)
string[] a3 = { "a", "b" };
int[] a4 = null; //no array
a4 = new[] { 10, 20, 30 };

for (int i = 0; i < a2.Length; i++) a2[i] = i; //set values
for (int i = 0; i < a2.Length; i++) print.it(a2[i]); //get values
foreach (var v in a1) print.it(v); //get values with foreach
print.it(a1[0], a1[^1]); //get the first and the last element
print.it(a1.Contains(7), Array.IndexOf(a1, 7)); //some array functions

/// Arrays can have multiple dimensions. Array elements can be arrays of different length. But these aren't often used.

/// Arrays have fixed size. If need to add/remove elements, use <google C# 'List T class'>List<> instead.

List<int> k1 = new() { 1, 3, 5, 7, 9 }; //list variable k1 has 5 elements of type int
var k2 = new List<int> { 1, 3, 5, 7, 9 }; //the same
List<string> k3 = new(); //create empty list
var k4 = new List<string>(); //the same

k1.Add(11); //append 1 element with value 11
k1.Insert(0, -1); //at index 0 insert 1 element with value -1
k1.RemoveAt(k1.Count - 1); //remove the last element
k1.Clear(); //remove all elements
print.it(k2.Contains(5), k2.IndexOf(5));
int[] a10 = k2.ToArray(); //create new array and copy elements

/// List elements are accessed (get, set) like array elements.

for (int i = 0; i < k1.Count; i++) print.it(k1[i]); //get values

/// <google C# 'Stack T class'>Stack<> is a list where you get/remove the last item.

var k = new Stack<int>();
k.Push(4); //append 1 element with value 4
while (k.TryPop(out var v)) print.it(v); //get and remove elements starting from the last

/// <google C# 'Queue T class'>Queue<> is a list where you get/remove the first item.

var q = new Queue<int>();
q.Enqueue(4); //append 1 element with value 4
while (q.TryDequeue(out var v)) print.it(v); //get and remove elements starting from the first

/// If need to use a collection in multiple threads simultaneously, use <b><+lang lock statement>lock<><>.

lock (k1) {
	k1.Add(10);
}

/// If need a variable that holds multiple values of different types, use one of:
/// - <+lang value tuple types>Tuple<>.
/// - Create a <google C# class>class</google> or <google C# struct>struct</google> with public fields.
/// - If need to access elements by index or key, use array/List/Dictionary/etc of type <.k>object<>.
