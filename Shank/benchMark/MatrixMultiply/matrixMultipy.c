typedef struct row {
  long cells[100];
} row;

typedef struct matix {
  row rows[100];
} matrix;
void matrixMultiply(matrix a, matrix b, matrix *c) {
  int i, j, k, sum = 0;
  for (i = 0; i < 99; i++) {
    for (j = 0; j < 99; j++) {
      for (k = 0; k < 99; k++) {
        sum += a.rows[i].cells[k] * b.rows[k].cells[j];
      }
      c->rows[i].cells[j] = sum;
    }
  }
}
int main() {
  matrix a, b, c;
  // TODO: simulate printing or remove it from shank example
  matrixMultiply(a, b, &c);
}
