public class TempProgram {
    public static void main(String[] args) {
        System.out.println("Hello, world!");
        
        int a = 5;
        int b = 10;
        int sum = add(a, b);
        System.out.println(a + " + " + b + " = " + sum);
    }
    
    // Jednoduchá funkcia na sčítanie dvoch čísel
    public static int add(int x, int y) {
        return x + y;
    }
}
