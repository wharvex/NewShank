#include <stdio.h>

/*
 * Setting INNER_LOOP_LEN to 44 causes the inner loop to calculate the 46th
 * Fibonacci number: 1836311903
 * See: https://planetmath.org/listoffibonaccinumbers
 * The 46th Fibonacci number was chosen as the target because it is the largest
 * Fibonacci number that is smaller than INT_MAX (2147483647).
 */
#define INNER_LOOP_LEN 44

#define OUTER_LOOP_LEN 70000000

int main() {
  int curr = 1;
  for (int j = 0; j < OUTER_LOOP_LEN; j++) {
    int prev1 = 1;
    int prev2 = 1;
    curr = 1;
    for (int i = 0; i < INNER_LOOP_LEN; i++) {
      curr = prev1 + prev2;
      prev2 = prev1;
      prev1 = curr;
    }
  }
  printf("%d", curr);
  return 0;
}
