#include <stdio.h>

#define INNER_LOOP_LEN 88
#define OUTER_LOOP_LEN 100000000

int main() {
  unsigned long long curr = 1;
  for (int j = 0; j < OUTER_LOOP_LEN; j++) {
    unsigned long long prev1 = 1;
    unsigned long long prev2 = 1;
    curr = 1;
    // This results in the 90th fibonacci number: 2880067194370816120
    // See: https://planetmath.org/listoffibonaccinumbers
    for (int i = 0; i < INNER_LOOP_LEN; i++) {
      curr = prev1 + prev2;
      prev2 = prev1;
      prev1 = curr;
    }
  }
  printf("%llu", curr);
  return 0;
}
