public class TempProgram {
    public static void main(String[] args) {
        // Dopln do poľa priezviská cvičiacich, ktorí učia Informatiku 1.
        String[] priezviska = { "Janech", "Meško", "Tóth",
                                "Kvet", "Ďuračík", "Gregorová", "Petríková" };
        
        // Prejdi polem a vypíš každé priezvisko do terminálu.
        for (int i = 0; i < priezviska.length; i++) {
            System.out.println(priezviska[i]);
        }
    }
}
