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
	.cfi_offset ra, -8
	li	a0, 1
	sd	a0, 48(sp)
	li	a1, 20
	sd	a1, 40(sp)
	sd	a0, 24(sp)
	sd	a0, 16(sp)
	li	a0, 1
	call	write@plt
	ld	a0, 16(sp)
	call	write@plt
	ld	a0, 48(sp)
.LBB0_1:                                # %for.condition
                                        # =>This Inner Loop Header: Depth=1
	sd	a0, 32(sp)
	ld	a0, 32(sp)
	ld	a1, 40(sp)
	bge	a0, a1, .LBB0_3
# %bb.2:                                # %for.body
                                        #   in Loop: Header=BB0_1 Depth=1
	ld	a0, 24(sp)
	ld	a1, 16(sp)
	add	a0, a0, a1
	sd	a0, 8(sp)
	call	write@plt
	ld	a0, 24(sp)
	ld	a1, 8(sp)
	ld	a2, 32(sp)
	sd	a0, 16(sp)
	sd	a1, 24(sp)
	addi	a0, a2, 1
	j	.LBB0_1
.LBB0_3:                                # %exit
	li	a0, 0
	ld	ra, 56(sp)                      # 8-byte Folded Reload
	addi	sp, sp, 64
	ret
.Lfunc_end0:
	.size	start, .Lfunc_end0-start
	.cfi_endproc
                                        # -- End function
	.section	".note.GNU-stack","",@progbits
