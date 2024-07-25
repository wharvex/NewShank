typedef struct row {
  long cells[586];
} row;

typedef struct matix {
  row rows[586];
} matrix;

void initMatrix(matrix *c, int seed) {
  int i, j = 0;
  for (i = 0; i < 585; i++) {
    for (j = 0; j < 585; j++) {
      c->rows[i].cells[j] = seed * (i + j);
    }
  }
}
void matrixMultiply(matrix *a, matrix *b, matrix *c) {
  int i, j, k, sum = 0;
  for (i = 0; i < 585; i++) {
    for (j = 0; j < 585; j++) {
      for (k = 0; k < 585; k++) {
        sum += a->rows[i].cells[k] * b->rows[k].cells[j];
      }
      c->rows[i].cells[j] = sum;
    }
  }
}
int main() {
  matrix a, b, c;
  initMatrix(&a, 1);
  initMatrix(&b, 3);
  matrixMultiply(&a, &b, &c);
}
