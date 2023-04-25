	.text
	.attribute	4, 16
	.attribute	5, "rv64i2p0"
	.file	"main"
	.globl	start                           # -- Begin function start
	.p2align	2
	.type	start,@function
start:                                  # @start
	.cfi_startproc
# %bb.0:                                # %entry
	addi	sp, sp, -64
	.cfi_def_cfa_offset 64
	sd	ra, 56(sp)                      # 8-byte Folded Spill
	sd	s0, 48(sp)                      # 8-byte Folded Spill
	.cfi_offset ra, -8
	.cfi_offset s0, -16
	addi	s0, sp, 64
	.cfi_def_cfa s0, 0
	li	a0, 1
	sd	a0, -24(s0)
	li	a1, 20
	sd	a1, -32(s0)
	sd	a0, -48(s0)
	sd	a0, -56(s0)
	addi	a0, s0, -48
	call	write@plt
	addi	a0, s0, -56
	call	write@plt
	mv	a0, sp
	addi	sp, a0, -16
	sw	zero, -12(a0)
	sw	zero, -16(a0)
	addi	a0, s0, -48
	call	write@plt
	addi	sp, s0, -64
	ld	ra, 56(sp)                      # 8-byte Folded Reload
	ld	s0, 48(sp)                      # 8-byte Folded Reload
	addi	sp, sp, 64
	ret
.Lfunc_end0:
	.size	start, .Lfunc_end0-start
	.cfi_endproc
                                        # -- End function
	.section	".note.GNU-stack","",@progbits
